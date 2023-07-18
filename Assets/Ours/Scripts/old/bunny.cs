using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Mathematics;
using static Unity.Mathematics.math;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
unsafe public class bunny : MonoBehaviour {

    [DllImport("meshinator")]
    unsafe private static extern void passBunnyMesh(void *vertex_positions, void *triangle_indices, void *vertex_normals);

    [DllImport("meshinator")]
    unsafe private static extern int num_verts_bunny();

    [DllImport("meshinator")]
    unsafe private static extern int num_triangles_bunny();

    Mesh mesh;

    void Update () {
        //Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        //Mesh.MeshData meshData = meshDataArray[0];
        //Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        //logMesh(positions_ptr, 3);
        int vertexAttributeCount = 2;
        int vertexCount = num_verts_bunny();
        int triangleIndexCount = num_triangles_bunny() * 3;

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];
        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
            vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        //position
        vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
        //normal
        vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3, stream: 1);

        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose();

        /**NativeArray<float3> positions = meshData.GetVertexData<float3>(0);
        positions[0] = 0f;
        positions[1] = right();
        positions[2] = up();

        NativeArray<float3> normals = meshData.GetVertexData<float3>(1);
        normals[0] = normals[1] = normals[2] = back();

        NativeArray<float4> tangents = meshData.GetVertexData<float4>(2);
        tangents[0] = tangents[1] = tangents[2] = float4(1f, 0f, 0f, -1f);
        **/

        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt16);
        /**
        NativeArray<ushort> triangleIndices = meshData.GetIndexData<ushort>();
        triangleIndices[0] = 0;
        triangleIndices[1] = 2;
        triangleIndices[2] = 1;
        **/

        passBunnyMesh(
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(0)),
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetIndexData<ushort>()),
            NativeArrayUnsafeUtility.GetUnsafePtr(meshData.GetVertexData<float3>(1))
        );

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount));

        mesh = new Mesh {
            name = "Procedural Mesh"
        };


        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        GetComponent<MeshFilter>().mesh = mesh;
    }
}