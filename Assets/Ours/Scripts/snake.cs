using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Mathematics;
using static Unity.Mathematics.math;

using System.Threading;

public enum State {NodeDragging, TargetDragging, Relaxing, Starting, Hovering, Static};

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
unsafe public class snake : MonoBehaviour {

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    static public extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32")]
    static public extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static public extern bool FreeLibrary(IntPtr hModule);

    IntPtr library;

    delegate void cpp_init();
    delegate int cpp_getNumVertices();
    delegate int cpp_getNumTriangles();
    delegate void cpp_reset();
    //delegate void cpp_test();

    delegate void cpp_solve(
        int num_feature_points,
        void *_targetEnabled__BOOL_ARRAY,
        void *_targetPositions__FLOAT3_ARRAY, //node pos
        void *vertex_positions__FLOAT3_ARRAY,
        void *vertex_normals__FLOAT3_ARRAY,
        void *triangle_indices__UINT_ARRAY,
        void *feature_point_positions__FLOAT3__ARRAY); // pos on snake
    
    delegate bool cpp_castRay(
        float ray_origin_x,
        float ray_origin_y,
        float ray_origin_z,
        float ray_direction_x,
        float ray_direction_y,
        float ray_direction_z,
        void *intersection_position__FLOAT_ARRAY__LENGTH_3,
        bool pleaseSetFeaturePoint,
        int indexOfFeaturePointToSet,
        void *feature_point_positions__FLOAT3__ARRAY = null);

    cpp_init init;
    cpp_getNumVertices getNumVertices;
    cpp_getNumTriangles getNumTriangles;
    cpp_reset reset;
    cpp_solve solve;
    cpp_castRay castRay;
    //cpp_test test;

    void LoadDLL() {
        library = LoadLibrary("Assets/snake");
        init            = (cpp_init)            Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_init"),            typeof(cpp_init));
        getNumVertices  = (cpp_getNumVertices)  Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getNumVertices"),  typeof(cpp_getNumVertices));
        getNumTriangles = (cpp_getNumTriangles) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_getNumTriangles"), typeof(cpp_getNumTriangles));
        reset           = (cpp_reset)           Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_reset"),           typeof(cpp_reset));
        solve           = (cpp_solve)           Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_solve"),           typeof(cpp_solve));
        castRay         = (cpp_castRay)         Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_castRay"),         typeof(cpp_castRay));
        //test            = (cpp_test)            Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_test"),            typeof(cpp_test));
    }

    public GameObject node_1;
    public GameObject head;
    public GameObject targetPrefab;
    public GameObject leftHand;
    public GameObject rightHand;
    public GameObject interactionDotRight;
    public GameObject interactionDotLeft;
    public GameObject[] targets;
    public NodeManager nodeManager;
    public Vector3 restPos;
    public UnityEngine.XR.InputFeatureUsage<float> fl;
    public State curState;

    public const float radius = 0.05f;

    public GameObject dragon_head;
    public GameObject dragon_body;
    DragonMeshManager DMM;
    const int HEAD = 0;
    const int BODY = 1;

    Vector3 node_pos;
    
    NativeArray<float> intersection_position;
    NativeArray<float3> posOnSnake;

    //place node
    bool  leftTriggerHeld = false; 
    bool rightTriggerHeld = false; 
    //drag node
    bool  leftGripHeld    = false; 
    bool rightGripHeld    = false; 
    //reset
    bool   yButtonHeld    = false;
    bool   bButtonHeld    = false;
    //delete
    bool   xButtonHeld    = false;
    bool   aButtonHeld    = false;
    //hide nodes
    bool leftStickHeld    = false;
    bool uiShowing = true;

    void ASSERT(bool b) {
        if (!b) {
            print("ASSERT");
            int[] foo = {};
            foo[42] = 0;
        }
    }

    // void Wrapper() {
    //     test();
    // }

    void Awake () {
        LoadDLL();

        init();
        intersection_position = new NativeArray<float>(3, Allocator.Persistent);
        posOnSnake = new NativeArray<float3>(nodeManager.numNodes, Allocator.Persistent);
        targets = new GameObject[nodeManager.numNodes];
        targets[0] = head;
        curState = State.Relaxing;
        DMM = new DragonMeshManager(dragon_head, dragon_body);
        DMM.SetUpAll();
        Update(); 
        curState = State.Starting;
    }

    void Update () {
        //init access vars
        Vector3  leftRayOrigin;
        Vector3 rightRayOrigin;
        Vector3  leftRayDirection;
        Vector3 rightRayDirection;
        //place node
        bool  leftTriggerPressed;
        bool rightTriggerPressed;
        //drag node
        bool     leftGripPressed;
        bool    rightGripPressed;
        bool     leftGripReleased;
        bool    rightGripReleased;
        //reset
        bool      yButtonPressed;
        bool      bButtonPressed;
        //delete
        bool      xButtonPressed; 
        bool      aButtonPressed;
        //Handle UI
        bool    leftStickPressed;
        bool    leftStickReleased;
        {
            leftRayDirection  =  leftHand.transform.rotation * Vector3.forward;
            rightRayDirection = rightHand.transform.rotation * Vector3.forward;
            {
                leftRayOrigin = new Vector3(); // FORNOW
                bool found = false;
                foreach (Transform child in leftHand.transform) {
                    if (child.name == "[Ray Interactor] Ray Origin") {
                        leftRayOrigin = child.position;
                        found = true;
                        break;
                    }
                }
                ASSERT(found);
            }
            {
                rightRayOrigin = new Vector3(); // FORNOW
                bool found = false;
                foreach (Transform child in rightHand.transform) {
                    if (child.name == "[Ray Interactor] Ray Origin") {
                        rightRayOrigin = child.position;
                        found = true;
                        break;
                    }
                }
                ASSERT(found);
            }
            {
                float value;
                bool  leftTriggerTemp =  leftTriggerHeld; 
                bool rightTriggerTemp = rightTriggerHeld;
                bool     leftGripTemp =     leftGripHeld; 
                bool    rightGripTemp =    rightGripHeld;

                 leftTriggerHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out value) && value >= 0.1f;
                rightTriggerHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out value) && value >= 0.1f;
                    leftGripHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip,    out value) && value >= 0.1f;
                   rightGripHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip,    out value) && value >= 0.1f;
               
                 leftTriggerPressed = ( !leftTriggerTemp &&  leftTriggerHeld); 
                rightTriggerPressed = (!rightTriggerTemp && rightTriggerHeld);
                    leftGripPressed = (    !leftGripTemp &&     leftGripHeld); 
                   rightGripPressed = (   !rightGripTemp &&    rightGripHeld);
                   leftGripReleased = (     leftGripTemp &&    !leftGripHeld); 
                  rightGripReleased = (    rightGripTemp &&   !rightGripHeld);
            }
            {
                bool aTemp  =   aButtonHeld;
                bool bTemp  =   bButtonHeld;
                bool xTemp  =   xButtonHeld;
                bool yTemp  =   yButtonHeld;
                bool lsTemp = leftStickHeld; 

                bool value;
                aButtonHeld   = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton       , out value) && value;
                bButtonHeld   = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton     , out value) && value;
                xButtonHeld   = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton       , out value) && value;
                yButtonHeld   = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton     , out value) && value;
                leftStickHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand ).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick  , out value) && value;

                aButtonPressed    = (! aTemp &&    aButtonHeld);
                bButtonPressed    = (! bTemp &&    bButtonHeld);
                xButtonPressed    = (! xTemp &&    xButtonHeld);
                yButtonPressed    = (! yTemp &&    yButtonHeld);
                leftStickPressed  = (!lsTemp &&  leftStickHeld);
                leftStickReleased = ( lsTemp && !leftStickHeld);
            }
        }

        //find any sphere interactions  
        int rightSelectedNodeIndex;
         int leftSelectedNodeIndex;
        int rightSelectedTargetIndex;
         int leftSelectedTargetIndex;
        {
            rightSelectedNodeIndex = -1;
             leftSelectedNodeIndex = -1;
            rightSelectedTargetIndex = -1;
             leftSelectedTargetIndex = -1;

            rightSelectedNodeIndex = SphereCast(rightRayOrigin, rightRayDirection, true);
             leftSelectedNodeIndex = SphereCast(leftRayOrigin,   leftRayDirection, true);
            rightSelectedTargetIndex = SphereCast(rightRayOrigin, rightRayDirection, false);
             leftSelectedTargetIndex = SphereCast(leftRayOrigin,   leftRayDirection, false);
        }

        //mesh gen
        if (curState == State.NodeDragging || curState == State.Relaxing) {

            node_pos = node_1.transform.position; // ???

            int triangleIndexCount = getNumTriangles() * 3; 

            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];
            { // TODO: link to whatever tutorial/docs we got this stuff from
                int vertexCount = getNumVertices();
                int vertexAttributeCount = 2;

                var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(vertexAttributeCount, Allocator.Temp);
                vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3); // position?
                vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3, stream: 1);
                meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
                vertexAttributes.Dispose();

                meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);
            }


            NativeArray<int> nativeBools = new NativeArray<int>(nodeManager.numNodes, Allocator.Temp);
            NativeArray<float3> nativeTargetPos = new NativeArray<float3>(nodeManager.numNodes, Allocator.Temp);
            {
                bool[] bools = nodeManager.getBools();
                for(int k = 0; k < nodeManager.numNodes; k++){
                    if(bools[k]) nativeBools[k] = 1;
                    else nativeBools[k] = 0;
                    nativeTargetPos[k] = new float3 (nodeManager.nodes[k].transform.position.x, nodeManager.nodes[k].transform.position.y, nodeManager.nodes[k].transform.position.z);
                }
            }
            solve(
                nodeManager.numNodes,
                NativeArrayUnsafeUtility.GetUnsafePtr(nativeBools),
                NativeArrayUnsafeUtility.GetUnsafePtr(nativeTargetPos),
                NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(0)),
                NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(1)),
                NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetIndexData<int>()),
                NativeArrayUnsafeUtility.GetUnsafePtr(posOnSnake));
            nativeBools.Dispose();
            nativeTargetPos.Dispose();

            for(int k = 0; k < nodeManager.nextAvalible; k++){
                //if(!(leftSelectedTargetIndex == k || rightSelectedNodeIndex == k)) 
                targets[k].transform.position = posOnSnake[k];
            }


            // FORNOW: TODO: try moving this before scope if we ever start passing triangle indices only once (e.g., in Awake)
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount));

            Mesh mesh = new Mesh {
                name = "Procedural Mesh"
            };
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().mesh = mesh;
        }

        DMM.UpdateAll();

        //cast interaction dots
        {
            //left cast
            if (castRay(
                leftRayOrigin.x,leftRayOrigin.y,leftRayOrigin.z,
                leftRayDirection.x,leftRayDirection.y,leftRayDirection.z,
                NativeArrayUnsafeUtility.GetUnsafePtr(intersection_position),
                false, -1, NativeArrayUnsafeUtility.GetUnsafePtr(posOnSnake))) {
                interactionDotLeft.SetActive(true);
                Vector3 dotPos = new Vector3(intersection_position[0], intersection_position[1], intersection_position[2]);
                if(leftSelectedTargetIndex != -1 && curState == State.TargetDragging) {
                    posOnSnake[leftSelectedTargetIndex] = dotPos;
                    targets[leftSelectedTargetIndex].transform.position = dotPos;
                }
                interactionDotLeft.transform.position = dotPos;
            }
            else{
                interactionDotLeft.SetActive(false);
            }

            //right cast
            if (castRay(
                rightRayOrigin.x,rightRayOrigin.y,rightRayOrigin.z,
                rightRayDirection.x,rightRayDirection.y,rightRayDirection.z,
                NativeArrayUnsafeUtility.GetUnsafePtr(intersection_position),
                false, -1)) {
                interactionDotRight.SetActive(true);
                interactionDotRight.transform.position = new Vector3(intersection_position[0], intersection_position[1], intersection_position[2]);
            }
            else{
                interactionDotRight.SetActive(false);
            }
        }
        //button control
        {
                 //generate new target node
            if(leftTriggerPressed || rightTriggerPressed)
            {
                GenNode(leftTriggerPressed, rightTriggerPressed);
            } //reset
            else if(yButtonPressed || bButtonPressed)
            {
                nodeManager.Setup();
                reset();
                for(int k = 1; k < nodeManager.numNodes; k++){
                    if(targets[k] != null) {
                        Destroy(targets[k]);
                    }
                }
                curState = State.Relaxing;
                Update(); 
                curState = State.Starting;         
                nodeManager.nodes[0].SetActive(true);
            } //delete and drag
            else if(leftSelectedNodeIndex != -1 || rightSelectedNodeIndex != -1 || leftSelectedTargetIndex != -1 || rightSelectedTargetIndex != -1)
            {
                bool nodeOrTarget = (leftSelectedNodeIndex != -1 || rightSelectedNodeIndex != -1);
                bool left = nodeOrTarget ? (leftSelectedNodeIndex != -1) : (leftSelectedTargetIndex != -1);
                //delete
                if(nodeOrTarget && left ? xButtonPressed : aButtonPressed) { 
                    foreach(Transform child in nodeManager.nodes[left ? leftSelectedNodeIndex : rightSelectedNodeIndex].transform) {
                        if(child.name == "Lines") {
                            foreach(Transform child2 in child){
                                if(child2.GetComponent<LinePos>().head != null) child2.GetComponent<LinePos>().head.SetActive(false);
                            }
                        }
                    }
                    nodeManager.nodes[left ? leftSelectedNodeIndex : rightSelectedNodeIndex].SetActive(false);
                    curState = State.Relaxing;
                    if(nodeManager.AnyActive()) ExcuseToUseIEnumeratorAndCoroutinesEvenThoughThereAreDeffinitlyBetterWaysToDoThisAndThisIsntEvenSomthingThatIsVeryNecessaryToDo();
                } //drag 
                else if(left ? leftGripPressed : rightGripPressed) {
                    curState = nodeOrTarget ? State.NodeDragging : State.TargetDragging;
                } //release
                else if(left ? leftGripReleased : rightGripReleased) {
                    curState = nodeOrTarget ? State.Relaxing : State.Static;
                    if(nodeOrTarget) ExcuseToUseIEnumeratorAndCoroutinesEvenThoughThereAreDeffinitlyBetterWaysToDoThisAndThisIsntEvenSomthingThatIsVeryNecessaryToDo();
                }
                else curState = State.Relaxing;
            } //hide UI
            if(leftStickPressed)
            {
                uiShowing = !uiShowing;
                foreach(GameObject target in targets){
                    if(target != null) target.GetComponent<MeshRenderer>().enabled = uiShowing;
                }
                foreach(GameObject node in nodeManager.nodes){
                    if(node != null){
                        node.GetComponent<MeshRenderer>().enabled = uiShowing;
                        foreach(Transform child in node.transform) {
                            if(child.name == "Lines") {
                                foreach(Transform child2 in child){
                                    child2.GetComponent<LineRenderer>().enabled = uiShowing;
                                }
                            }
                        }
                    }
                }

            }
        }
        //clamp pos
    }

    public void GenNode(bool leftHandFire, bool rightHandFire) {
        if(gameObject.activeSelf && (nodeManager.nextAvalible != nodeManager.numNodes)){

            Vector3 ray_origin_r = new Vector3(); Vector3 ray_origin_l = new Vector3();
            ASSERT(rightHand!=null && leftHand!=null); 
            foreach(Transform child in rightHand.transform){if(child.name == "[Ray Interactor] Ray Origin") ray_origin_r = child.position;} 
            foreach(Transform child in leftHand.transform) {if(child.name == "[Ray Interactor] Ray Origin") ray_origin_l = child.position;}
            
            Vector3 ray_direction_r = rightHand.transform.rotation * Vector3.forward;
            Vector3 ray_direction_l = leftHand.transform.rotation  * Vector3.forward;

            if (castRay(ray_origin_r.x, ray_origin_r.y, ray_origin_r.z, ray_direction_r.x, ray_direction_r.y, ray_direction_r.z, NativeArrayUnsafeUtility.GetUnsafePtr(intersection_position), rightHandFire, nodeManager.nextAvalible) && rightHandFire) {
                InstantiateNode();
            }
            if (castRay(ray_origin_l.x, ray_origin_l.y, ray_origin_l.z, ray_direction_l.x, ray_direction_l.y, ray_direction_l.z, NativeArrayUnsafeUtility.GetUnsafePtr(intersection_position),  leftHandFire, nodeManager.nextAvalible) &&  leftHandFire) {
                InstantiateNode();
            }
        }
    }

    void InstantiateNode() {
        Vector3 pos = new Vector3(intersection_position[0], intersection_position[1], intersection_position[2]);                
        GameObject tar = Instantiate(targetPrefab, pos, Quaternion.identity);
        targets[nodeManager.nextAvalible] = tar;
        nodeManager.SetProperties(pos);
        foreach(Transform child in nodeManager.nodes[nodeManager.nextAvalible-1].transform) {
            if(child.name == "Lines") {
                foreach(Transform child2 in child){
                    child2.GetComponent<LinePos>().head = tar;
                }
            }
        }
    }

    int SphereCast(Vector3 origin, Vector3 direction, bool nodeOrTarget) {
        int indexToReturn = -1;
        for (int index = 0; index < (nodeOrTarget ? nodeManager.nodes.Length : (nodeManager.nextAvalible - 1)); ++index) {
            GameObject node = nodeOrTarget ? nodeManager.nodes[index] : targets[index];
            if (!node.activeSelf) { continue; }

            Vector3 center = node.transform.position;
            Vector3 oc = origin - center;
            float a = Vector3.Dot(direction, direction);
            float half_b = Vector3.Dot(oc, direction);
            float c = Vector3.Dot(oc, oc) - radius*radius;
            float discriminant = half_b * half_b  - a*c;
            if(discriminant > 0) {
                if (indexToReturn == -1) indexToReturn = index;
                else if((oc.magnitude < (origin - nodeManager.nodes[indexToReturn].transform.position).magnitude)) indexToReturn = index;
            }
            if(nodeOrTarget) Clamp(node);
        }
        return indexToReturn;
    }

    void Clamp(GameObject node){
        node.transform.GetComponent<Rigidbody>().velocity = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 new_pos = new Vector3(
            Mathf.Clamp(node.transform.position.x, -2.0f, 2.0f),
            Mathf.Clamp(node.transform.position.y, -2.0f, 2.0f), 
            Mathf.Clamp(node.transform.position.z, -2.0f, 2.0f)
            );

        node.transform.position = new_pos;
    }

    public void ExcuseToUseIEnumeratorAndCoroutinesEvenThoughThereAreDeffinitlyBetterWaysToDoThisAndThisIsntEvenSomthingThatIsVeryNecessaryToDo() {
        StartCoroutine(Relax(1.5f));
    }

    IEnumerator Relax(float sec){
        if(curState == State.Static || curState == State.NodeDragging) curState = State.Relaxing;
        yield return new WaitForSeconds(sec);
        if(!(curState == State.NodeDragging)) {curState = State.Static;}
    }

    void OnApplicationQuit () {
        { // FORNOW: uber sketchy delay because solve may not have finished writing data allocated by C# and if we quit out and then it writes we crash unity
            int N = 31623;
            int k;
            for (int i = 0; i < N; ++i) for (int j = 0; j < N; ++j) k = i + j;
        }

        FreeLibrary(library);
        posOnSnake.Dispose(); 
        intersection_position.Dispose();
    }
}

unsafe public class DragonMeshManager {

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    static public extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32")]
    static public extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static public extern bool FreeLibrary(IntPtr hModule);

    IntPtr library;

    delegate int cpp_dragon_getNumVertices(int mesh_index);
    delegate int cpp_dragon_getNumTriangles(int mesh_index);
    delegate int cpp_dragon_getNumBones();

    delegate void cpp_dragon_getMesh(
        int mesh_index,
        void* vertex_positions,
        void* vertex_normals,
        void* vertex_colors,
        void* triangle_indices);

    delegate void cpp_dragon_yzoBones(
        void *bones_y,
        void *bones_z,
        void *bones_o);

    delegate void cpp_dragon_initializeBones (
        void *bones_y,
        void *bones_z,
        void *bones_o,
        void *bone_indices,
        void *bone_weights);

    delegate void cpp_dragon_yzoHead (
        void *bones_y,
        void *bones_z,
        void *bones_o);

    cpp_dragon_getNumVertices dragon_getNumVertices;
    cpp_dragon_getNumTriangles dragon_getNumTriangles;
    cpp_dragon_getNumBones dragon_getNumBones;
    cpp_dragon_getMesh dragon_getMesh;
    cpp_dragon_yzoBones dragon_yzoBones;
    cpp_dragon_initializeBones dragon_initializeBones;
    cpp_dragon_yzoHead dragon_yzoHead;

    void LoadDLL() {
        library = LoadLibrary("Assets/snake");
        dragon_getNumVertices = (cpp_dragon_getNumVertices) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_getNumVertices"), typeof(cpp_dragon_getNumVertices));
        dragon_getNumTriangles = (cpp_dragon_getNumTriangles) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_getNumTriangles"), typeof(cpp_dragon_getNumTriangles));
        dragon_getNumBones = (cpp_dragon_getNumBones) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_getNumBones"), typeof(cpp_dragon_getNumBones));
        dragon_getMesh = (cpp_dragon_getMesh) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_getMesh"), typeof(cpp_dragon_getMesh));
        dragon_yzoBones = (cpp_dragon_yzoBones) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_yzoBones"), typeof(cpp_dragon_yzoBones));
        dragon_initializeBones = (cpp_dragon_initializeBones) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_initializeBones"), typeof(cpp_dragon_initializeBones));
        dragon_yzoHead = (cpp_dragon_yzoHead) Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, "cpp_dragon_yzoHead"), typeof(cpp_dragon_yzoHead));
    }

    private GameObject head;
    private GameObject body;

    const int HEAD = 0;
    const int BODY = 1;

    SkinnedMeshRenderer bodyRend;
    NativeArray<Vector3> bodyBones_y;
    NativeArray<Vector3> bodyBones_z;
    NativeArray<Vector3> bodyBones_o;
    Transform[] bodyBones;

    // Precondition: head and body gameobjects should have the following components:
    // - skinnedmeshrenderer
    // - material that supports vertex colors
    public DragonMeshManager(GameObject h, GameObject b) {
        head = h;
        body = b;
        LoadDLL();
    }

    ~DragonMeshManager() {
        FreeLibrary(library);
    }

    public void SetUpAll() {
        SetUp(HEAD);
        SetUp(BODY);
    }

    public void SetUp(int index) {
        GameObject dragon_object = (index == HEAD) ? head : body;

        int triangleIndexCount = dragon_getNumTriangles(index) * 3; 

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];
        int vertexCount = dragon_getNumVertices(index);

        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(3, Allocator.Temp);
        vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
        vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3, stream: 1);
        vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Color, dimension:4, stream: 2);
        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose();

        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);

        dragon_getMesh(
            index,
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(0)),
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(1)),
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<Color>(2)),
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetIndexData<int>()));

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount));

        string mesh_name = (index == HEAD) ? "Dragon Head" : "Dragon Body";
        
        Mesh dragon_mesh = new Mesh {
            name = mesh_name
        };

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, dragon_mesh);
        dragon_mesh.RecalculateBounds();
        dragon_object.GetComponent<SkinnedMeshRenderer>().sharedMesh = dragon_mesh;

        if (index == BODY) {
            int num_bones = dragon_getNumBones();
            int num_vertices = dragon_getNumVertices(BODY);

            bodyBones_y = new NativeArray<Vector3>(num_bones, Allocator.Temp);
            bodyBones_z = new NativeArray<Vector3>(num_bones, Allocator.Temp);
            bodyBones_o = new NativeArray<Vector3>(num_bones, Allocator.Temp);
            NativeArray<Vector4Int> cpp_bone_indices = new NativeArray<Vector4Int>(num_vertices, Allocator.Temp);
            NativeArray<Vector4> cpp_bone_weights = new NativeArray<Vector4>(num_vertices, Allocator.Temp);

            dragon_initializeBones (NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_y),
                                    NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_z),
                                    NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_o),
                                    NativeArrayUnsafeUtility.GetUnsafePtr(cpp_bone_indices),
                                    NativeArrayUnsafeUtility.GetUnsafePtr(cpp_bone_weights));

            Matrix4x4[] bindPoses = new Matrix4x4[num_bones];
            bodyRend = dragon_object.GetComponent<SkinnedMeshRenderer>();
            bodyBones = new Transform[num_bones];
            bindPoses = new Matrix4x4[num_bones];
            for (int boneIndex = 0; boneIndex < num_bones; boneIndex++) {


                bodyBones[boneIndex] = new GameObject("Bone " + boneIndex.ToString()).transform;
                bodyBones[boneIndex].parent = dragon_object.transform;
                bodyBones[boneIndex].localPosition = bodyBones_o[boneIndex];
                bodyBones[boneIndex].localRotation = Quaternion.LookRotation(bodyBones_z[boneIndex], bodyBones_y[boneIndex]);
                bindPoses[boneIndex] = Matrix4x4.identity;// bones[boneIndex].worldToLocalMatrix * transform.localToWorldMatrix;
            }
            bodyRend.sharedMesh.bindposes = bindPoses;
            bodyRend.bones = bodyBones;
            
            BoneWeight[] bone_weights = new BoneWeight[num_vertices];
            for (int i = 0; i < num_vertices; i++) {
                bone_weights[i].boneIndex0 = cpp_bone_indices[i][0];
                bone_weights[i].boneIndex1 = cpp_bone_indices[i][1];
                bone_weights[i].boneIndex2 = cpp_bone_indices[i][2];
                bone_weights[i].boneIndex3 = cpp_bone_indices[i][3];
                bone_weights[i].weight0 = cpp_bone_weights[i][0];
                bone_weights[i].weight1 = cpp_bone_weights[i][1];
                bone_weights[i].weight2 = cpp_bone_weights[i][2];
                bone_weights[i].weight3 = cpp_bone_weights[i][3];
            }

            bodyRend.sharedMesh.boneWeights = bone_weights;

            bodyBones_y.Dispose();
            bodyBones_z.Dispose();
            bodyBones_o.Dispose();
            cpp_bone_indices.Dispose();
            cpp_bone_weights.Dispose();

        }
    }

    public void UpdateAll() {
        Update(HEAD);
        Update(BODY);
    }

    public void Update(int index) {
        if (index == BODY) {
            int num_bones = dragon_getNumBones();

            bodyBones_y = new NativeArray<Vector3>(num_bones, Allocator.Temp);
            bodyBones_z = new NativeArray<Vector3>(num_bones, Allocator.Temp);
            bodyBones_o = new NativeArray<Vector3>(num_bones, Allocator.Temp);

            dragon_yzoBones(NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_y),
                            NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_z),
                            NativeArrayUnsafeUtility.GetUnsafePtr(bodyBones_o));
            for (int boneIndex = 0; boneIndex < num_bones; boneIndex++) {
                bodyBones[boneIndex].localPosition = bodyBones_o[boneIndex];
                bodyBones[boneIndex].localRotation = Quaternion.LookRotation(bodyBones_z[boneIndex], bodyBones_y[boneIndex]);

            }

            bodyBones_y.Dispose();
            bodyBones_z.Dispose();
            bodyBones_o.Dispose();

            bodyRend.sharedMesh.RecalculateBounds();

        }

        else if (index == HEAD) {
            NativeArray<float> head_y = new NativeArray<float>(3, Allocator.Temp);
            NativeArray<float> head_z = new NativeArray<float>(3, Allocator.Temp);
            NativeArray<float> head_o = new NativeArray<float>(3, Allocator.Temp);

            dragon_yzoHead(NativeArrayUnsafeUtility.GetUnsafePtr(head_y),
                           NativeArrayUnsafeUtility.GetUnsafePtr(head_z),
                           NativeArrayUnsafeUtility.GetUnsafePtr(head_o));

            head.transform.localPosition = new Vector3(head_o[0], head_o[1], head_o[2]);
            head.transform.localRotation = Quaternion.LookRotation(new Vector3(head_z[0], head_z[1], head_z[2]),
                                                         new Vector3(head_y[0], head_y[1], head_y[2]));

            head.GetComponent<SkinnedMeshRenderer>().sharedMesh.RecalculateBounds();
        }
    }

}