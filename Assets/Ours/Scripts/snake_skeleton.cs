/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class snake_skeleton : MonoBehaviour
{
    public MeshFilter snake_meshfilter;
    MeshFilter skin_meshfilter;
    Animation animation;
    SkinnedMeshRenderer renderer;
    Transform[] bones;
    int num_bones;
    Matrix4x4[] bindPoses;
    Vector3[] snake_vertices;
    // Start is called before the first frame update
    void Start()
    {
        skin_meshfilter = gameObject.GetComponent<MeshFilter>();
        animation = gameObject.GetComponent<Animation>();
        renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
        int snake_vertex_count =  snake_meshfilter.mesh.vertexCount;
        num_bones = snake_vertex_count/10;
        bones = new Transform[num_bones];
        bindPoses = new Matrix4x4[num_bones];
        snake_vertices = snake_meshfilter.mesh.vertices;
        for (int i = 0; i < num_bones; i++) {
            bones[i] = new GameObject("Bone " + i.ToString()).transform;
            bones[i].parent = transform;
            bones[i].localRotation = Quaternion.identity;
            bones[i].localPosition = Vector3.up * snake_vertices[i*10 + 9].y;
            bindPoses[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
        }
        skin_meshfilter.mesh.bindposes = bindPoses;
        renderer.bones = bones;
    }
    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < num_bones; i++) {
            bones[i].localPosition = snake_vertices[i*10 + 9];
            bones[i].localRotation = Quaternion.FromToRotation(Vector3.up, snake_vertices[(i+1)*10 + 9]);
        }
    }
}
*/
/**
using UnityEngine;
using System.Collections;
// this example creates a quad mesh from scratch, creates bones
// and assigns them, and animates the bones motion to make the
// quad animate based on a simple animation curve.
public class snake_skeleton : MonoBehaviour
{
    void Start()
    {
        gameObject.AddComponent<Animation>();
        gameObject.AddComponent<SkinnedMeshRenderer>();
        SkinnedMeshRenderer rend = GetComponent<SkinnedMeshRenderer>();
        Animation anim = GetComponent<Animation>();
        // Build basic mesh
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { new Vector3(-1, 0, 0), new Vector3(1, 0, 0), new Vector3(-1, 5, 0), new Vector3(1, 5, 0) };
        mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new int[] { 2, 3, 1, 2, 1, 0 };
        mesh.RecalculateNormals();
        rend.material = new Material(Shader.Find("Diffuse"));
        // assign bone weights to mesh
        BoneWeight[] weights = new BoneWeight[4];
        weights[0].boneIndex0 = 0;
        weights[0].weight0 = 1;
        weights[1].boneIndex0 = 0;
        weights[1].weight0 = 1;
        weights[2].boneIndex0 = 1;
        weights[2].weight0 = 1;
        weights[3].boneIndex0 = 1;
        weights[3].weight0 = 1;
        mesh.boneWeights = weights;
        // Create Bone Transforms and Bind poses
        // One bone at the bottom and one at the top
        Transform[] bones = new Transform[2];
        Matrix4x4[] bindPoses = new Matrix4x4[2];
        bones[0] = new GameObject("Lower").transform;
        bones[0].parent = transform;
        // Set the position relative to the parent
        bones[0].localRotation = Quaternion.identity;
        bones[0].localPosition = Vector3.zero;
        // The bind pose is bone's inverse transformation matrix
        // In this case the matrix we also make this matrix relative to the root
        // So that we can move the root game object around freely
        bindPoses[0] = bones[0].worldToLocalMatrix * transform.localToWorldMatrix;
        bones[1] = new GameObject("Upper").transform;
        bones[1].parent = transform;
        // Set the position relative to the parent
        bones[1].localRotation = Quaternion.identity;
        bones[1].localPosition = new Vector3(0, 5, 0);
        // The bind pose is bone's inverse transformation matrix
        // In this case the matrix we also make this matrix relative to the root
        // So that we can move the root game object around freely
        bindPoses[1] = bones[1].worldToLocalMatrix * transform.localToWorldMatrix;
        // bindPoses was created earlier and was updated with the required matrix.
        // The bindPoses array will now be assigned to the bindposes in the Mesh.
        mesh.bindposes = bindPoses;
        // Assign bones and bind poses
        rend.bones = bones;
        rend.sharedMesh = mesh;
        // Assign a simple waving animation to the bottom bone
        AnimationCurve curve = new AnimationCurve();
        curve.keys = new Keyframe[] { new Keyframe(0, 0, 0, 0), new Keyframe(1, 3, 0, 0), new Keyframe(2, 0.0F, 0, 0) };
        // Create the clip with the curve
        AnimationClip clip = new AnimationClip();
        clip.legacy = true;
        clip.SetCurve("Lower", typeof(Transform), "m_LocalPosition.z", curve);
        // Add and play the clip
        clip.wrapMode = WrapMode.Loop;
        anim.AddClip(clip, "test");
        anim.Play("test");
    }
}
**/
