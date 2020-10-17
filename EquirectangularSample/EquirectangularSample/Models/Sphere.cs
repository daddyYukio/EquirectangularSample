using System;
using System.Collections.Generic;

namespace EquirectangularSample
{
    //
    // http://www.songho.ca/opengl/gl_sphere.html
    //
    public class Sphere
    {
        class Vertex
        {
            public float x, y, z, s, t;
        };

        private float radius = 1.0f;
        private int sectorCount = 36;                        // longitude, # of slices
        private int stackCount = 18;                         // latitude, # of stacks

        private List<float> vertices = new List<float>();
        private List<float> texCoords = new List<float>();
        private List<uint> indices = new List<uint>();
        private List<float> interleavedVertices = new List<float>();

        public Sphere(float radius, int sectorCount, int stackCount)
        {
            this.radius = radius;
            this.sectorCount = sectorCount;
            this.stackCount = stackCount;

            BuildVertices();
        }

        public uint GetVertexCount()
        { 
            return (uint)vertices.Count / 3; 
        }
        

        public uint GetTexCoordCount()
        { 
            return (uint)texCoords.Count / 2; 
        }

        public uint GetIndexCount()
        { 
            return (uint)indices.Count; 
        }

        public uint GetVertexSize()
        { 
            return (uint)vertices.Count * sizeof(float); 
        }

        public uint GetTexCoordSize()
        { 
            return (uint)texCoords.Count * sizeof(float); 
        }

        public uint GetIndexSize()
        { 
            return (uint)indices.Count * sizeof(uint); 
        }

        public float[] GetVertices()
        { 
            return vertices.ToArray(); 
        }

        public float[] GetTexCoords()
        { 
            return texCoords.ToArray(); 
        }
        public uint[] GetIndices()
        { 
            return indices.ToArray(); 
        }

        public uint GetInterleavedVertexCount()
        { 
            return GetVertexCount();
        }    
        public uint GetInterleavedVertexSize()
        { 
            return (uint)interleavedVertices.Count * sizeof(float);
        }

        public int GetInterleavedVertexStride()
        {
            return (int)sizeof(float) * 5; // vertex x 3 + texture x 2
        }

        public int GetInterleavedVertexTexCoordOffset()
        {
            return (int)sizeof(float) * 3; // vertex x 3
        }

        public float[] GetInterleavedVertices()
        { 
            return interleavedVertices.ToArray(); 
        }

        private void ClearArrays()
        {
            vertices.Clear();
            texCoords.Clear();
            indices.Clear();
        }

        private void AddVertex(float x, float y, float z)
        {
            vertices.Add(x);
            vertices.Add(y);
            vertices.Add(z);
        }

        private void AddTexCoord(float s, float t)
        {
            texCoords.Add(s * -1); // 左右反転
            texCoords.Add(t);
        }

        private void AddIndices(uint i1, uint i2, uint i3)
        {
            indices.Add(i1);
            indices.Add(i2);
            indices.Add(i3);
        }

        private void BuildVertices()
        {
            var tmpVertices = new List<Vertex>();
            int i, j;

            float sectorStep = 2 * (float)Math.PI / sectorCount;
            float stackStep = (float)Math.PI / stackCount;
            float sectorAngle, stackAngle;

            // compute all vertices first, each vertex contains (x,y,z,s,t) except normal
            for(i = 0; i <= stackCount; ++i)
            {
                stackAngle = (float)Math.PI / 2 - i * stackStep;        // starting from pi/2 to -pi/2
                float xy = radius * (float)Math.Cos(stackAngle);       // r * cos(u)
                float z = radius * (float)Math.Sin(stackAngle);        // r * sin(u)

                // add (sectorCount+1) vertices per stack
                // the first and last vertices have same position and normal, but different tex coords
                for(j = 0; j <= sectorCount; ++j)
                {
                    sectorAngle = j* sectorStep;           // starting from 0 to 2pi

                    Vertex vertex = new Vertex();
                    vertex.x = xy* (float)Math.Cos(sectorAngle);      // x = r * cos(u) * cos(v)
                    vertex.y = xy* (float)Math.Sin(sectorAngle);      // y = r * cos(u) * sin(v)
                    vertex.z = z;                           // z = r * sin(u)
                    vertex.s = (float) j/sectorCount;        // s
                    vertex.t = (float) i/stackCount;         // t
                    tmpVertices.Add(vertex);
                }
            }

            // clear memory of prev arrays
            ClearArrays();

            var v1 = new Vertex();
            var v2 = new Vertex();
            var v3 = new Vertex();
            var v4 = new Vertex();
            var n = new List<float>();      // 1 face normal

            int vi1, vi2;
            uint index = 0;                                  // index for vertex
            for(i = 0; i<stackCount; ++i)
            {
                vi1 = i* (sectorCount + 1);                // index of tmpVertices
                vi2 = (i + 1) * (sectorCount + 1);

                for(j = 0; j<sectorCount; ++j, ++vi1, ++vi2)
                {
                    // get 4 vertices per sector
                    //  v1--v3
                    //  |    |
                    //  v2--v4
                    v1 = tmpVertices[vi1];
                    v2 = tmpVertices[vi2];
                    v3 = tmpVertices[vi1 + 1];
                    v4 = tmpVertices[vi2 + 1];

                    // if 1st stack and last stack, store only 1 triangle per sector
                    // otherwise, store 2 triangles (quad) per sector
                    if(i == 0) // a triangle for first stack ==========================
                    {
                        // put a triangle
                        AddVertex(v1.x, v1.y, v1.z);
                        AddVertex(v2.x, v2.y, v2.z);
                        AddVertex(v4.x, v4.y, v4.z);

                        // put tex coords of triangle
                        AddTexCoord(v1.s, v1.t);
                        AddTexCoord(v2.s, v2.t);
                        AddTexCoord(v4.s, v4.t);

                        // put indices of 1 triangle
                        AddIndices(index, index+1, index+2);

                        index += 3;     // for next
                    }
                    else if(i == (stackCount-1)) // a triangle for last stack =========
                    {
                        // put a triangle
                        AddVertex(v1.x, v1.y, v1.z);
                        AddVertex(v2.x, v2.y, v2.z);
                        AddVertex(v3.x, v3.y, v3.z);

                        // put tex coords of triangle
                        AddTexCoord(v1.s, v1.t);
                        AddTexCoord(v2.s, v2.t);
                        AddTexCoord(v3.s, v3.t);

                        // put indices of 1 triangle
                        AddIndices(index, index+1, index+2);

                        index += 3;     // for next
                    }
                    else // 2 triangles for others ====================================
                    {
                        // put quad vertices: v1-v2-v3-v4
                        AddVertex(v1.x, v1.y, v1.z);
                        AddVertex(v2.x, v2.y, v2.z);
                        AddVertex(v3.x, v3.y, v3.z);
                        AddVertex(v4.x, v4.y, v4.z);

                        // put tex coords of quad
                        AddTexCoord(v1.s, v1.t);
                        AddTexCoord(v2.s, v2.t);
                        AddTexCoord(v3.s, v3.t);
                        AddTexCoord(v4.s, v4.t);

                        // put indices of quad (2 triangles)
                        AddIndices(index, index+1, index+2);
                        AddIndices(index+2, index+1, index+3);

                        index += 4;     // for next
                    }
                }
            }

            // generate interleaved vertex array as well
            BuildInterleavedVertices();
        }

        private void BuildInterleavedVertices()
        {
            interleavedVertices.Clear();

            int i, j;
            int count = vertices.Count;

            for (i = 0, j = 0; i < count; i += 3, j += 2)
            {
                interleavedVertices.Add(vertices[i]);
                interleavedVertices.Add(vertices[i + 1]);
                interleavedVertices.Add(vertices[i + 2]);

                interleavedVertices.Add(texCoords[j]);
                interleavedVertices.Add(texCoords[j + 1]);
            }

        }
    }
}
