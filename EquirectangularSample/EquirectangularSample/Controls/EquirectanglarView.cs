using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace EquirectangularSample.Controls
{
    public class EquirectanglarView : View
    {
        public byte[] TextureImage { get; set; }

        public string VertexShader { get; private set; }

        public string FragmentShader { get; private set; }

        public Sphere Sphere { get; private set; }

        public delegate void ElementSizeChangedEvent(double w, double h);

        public ElementSizeChangedEvent ElementSizeChanged;

        public EquirectanglarView()
        {
            this.Sphere = new Sphere(1.0f, 144, 72);

            this.VertexShader =
                "#version 300 es                                  \n" +
                "in vec4 position;                                \n" +
                "in vec2 texcoord;                                \n" +
                "out vec2 textureCoordinate;                      \n" +
                "uniform mat4 projection;                         \n" +
                "void main()                                      \n" +
                "{                                                \n" +
                "    gl_Position = projection * position;         \n" +
                "    textureCoordinate = texcoord;                \n" +
                "}                                                \n";

            this.FragmentShader =
                "#version 300 es                                  \n" +
                "in highp vec2 textureCoordinate;                 \n" +
                "out lowp vec4 fragColor;                         \n" +
                "uniform lowp sampler2D text;                     \n" +
                "void main()                                      \n" +
                "{                                                \n" +
                "    fragColor = texture(text, textureCoordinate);\n" +
                "}                                                \n";
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            ElementSizeChanged?.Invoke(width, height);
        }

    }
}
