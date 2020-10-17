using System;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;

using EquirectangularSample;

#if __ANDROID__
using Android.Content;
	using Android.Graphics;
	using Android.Util;
	using Android.Views;
	using OpenTK.Platform.Android;
#endif

#if __IOS__
	using CoreGraphics;
	using Foundation;
	using ObjCRuntime;
	using OpenGLES;
	using OpenTK.Platform.iPhoneOS;
	using UIKit;
#endif

namespace Shared
{
#if __ANDROID__
	public class OGLView : AndroidGameView
#endif

#if __IOS__
	public class OGLView : iPhoneOSGameView
#endif
	{
		private int _textureId = 0;
		
		private int _program = 0;

		private int _uniformProjection;
		private int _uniformTexture;

		private const int _attributeVertex = 0;
		private const int _attributeTextcoord = 1;

		private int _vbo, _vbi;

		private float _viewAngleX, _viewAngleY;

		private const float MAX_FOVY = 80.0f;
		private const float MIN_FOVY = 20.0f;
		private float _fovy = 80.0f;

		private Matrix4 view = new Matrix4();
		private Matrix4 projection = new Matrix4();

		public Sphere Sphere { get; set; }

		private byte[] _textureData;

		public byte[] TextureImage
		{
			set
			{
				_textureData = value;

				if (IsInitialized)
				{
					LoadTexture(_textureId, _textureData);
					SetupProjection();
					RenderSphere();
				}
			}
		}

		public string VertexShader { get; set; }

		public string FragmentShader { get; set; }


		float prevx, prevy;

		private bool _isInitialized = false;
		public bool IsInitialized
		{
			get => _isInitialized;
		}

#if __ANDROID__
		public OGLView(Context context) : base(context)
		{
			_viewAngleX = 0;
			_viewAngleY = 0;

			Resize += OnResize;
		}
#endif

#if __IOS__
		[Export("layerClass")]
		public static Class LayerClass()
		{
			//
			// UIViewのレイヤーではなく、OpenGL ES専用のレイヤーをセットする
			//
			// https://ameblo.jp/program-boy/entry-11416852298.html
			//
			return iPhoneOSGameView.GetLayerClass();
		}

		public OGLView(CGRect frame) : base(frame)
		{
			_viewAngleX = 0;
			_viewAngleY = 0;

			Resize += OnResize;

			//
			// iOSでは OnLoad等もコールされないため、CreateFrameBufferを明示的にコールする
			LayerRetainsBacking = true;
			ContextRenderingApi = EAGLRenderingAPI.OpenGLES3;
			LayerColorFormat = EAGLColorFormat.RGBA8;
			CreateFrameBuffer();
		}
#endif

		private void OnResize(object sender, EventArgs e)
		{
			if (IsInitialized)
			{
				SetupProjection();
				RenderSphere();
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			//
			// iOSでは　OnLoadがコールされないので、
			// ViewRendererで明示的にInitializeをコールする必要がある
			//
			Initialize();
		}

		public bool Initialize()
		{
			if (Sphere == null || _textureData == null || VertexShader == null || FragmentShader == null)
			{
				return false;
			}

			if (Size.Width == 0 || Size.Height == 0)
			{
				return false;
			}

			MakeCurrent();

			GL.ClearColor(0, 0, 0, 1);

			GL.ClearDepth(1.0f);
			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Lequal);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Front);

			_textureId = GL.GenTexture();
			LoadTexture(_textureId, _textureData);

			LoadShaders(VertexShader, FragmentShader, out _program);

			SetupProjection();

			InitModel();

			RenderSphere();

			_isInitialized = true;

			return true;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_textureId != 0)
			{
				GL.DeleteTexture(_textureId);
			}

			if (_program != 0)
			{
				GL.DeleteProgram(_program);
				_program = 0;
			}
		}

#if __ANDROID__
		//
		// Androidでは、初期化処理が内部的に行われるので、
		// FrameBufferの指定も、メソッドのオーバーライドで行う
		// ※iOSでは、コンストラクタで明示的にCreateFrameBufferをコールする
		//
		// 以下を参考に実装
		// https://docs.microsoft.com/en-us/samples/xamarin/monodroid-samples/texturedcube-10/
		//
		protected override void CreateFrameBuffer()
		{
			ContextRenderingApi = GLVersion.ES3;
			
			try
			{
				Log.Verbose("TexturedCube", "Loading with high quality settings");

				GraphicsMode = new GraphicsMode(new ColorFormat(32), 24, 0, 0);
				base.CreateFrameBuffer();
				return;
			}
			catch (Exception ex)
			{
				Log.Verbose("TexturedCube", "{0}", ex);
			}

			// the default GraphicsMode that is set consists of (16, 16, 0, 0, 2, false)
			try
			{
				Log.Verbose("TexturedCube", "Loading with default settings");

				base.CreateFrameBuffer();
				return;
			}
			catch (Exception ex)
			{
				Log.Verbose("TexturedCube", "{0}", ex);
			}

			// Fallback modes
			// If the first attempt at initializing the surface with a default graphics
			// mode fails, then the app can try different configurations. Devices will
			// support different modes, and what is valid for one might not be valid for
			// another. If all options fail, you can set all values to 0, which will
			// ask for the first available configuration the device has without any
			// filtering.
			// After a successful call to base.CreateFrameBuffer(), the GraphicsMode
			// object will have its values filled with the actual values that the
			// device returned.


			// This is a setting that asks for any available 16-bit color mode with no
			// other filters. It passes 0 to the buffers parameter, which is an invalid
			// setting in the default OpenTK implementation but is valid in some
			// Android implementations, so the AndroidGraphicsMode object allows it.
			try
			{
				Log.Verbose("TexturedCube", "Loading with custom Android settings (low mode)");
				GraphicsMode = new AndroidGraphicsMode(16, 0, 0, 0, 0, false);

				// if you don't call this, the context won't be created
				base.CreateFrameBuffer();
				return;
			}
			catch (Exception ex)
			{
				Log.Verbose("TexturedCube", "{0}", ex);
			}

			// this is a setting that doesn't specify any color values. Certain devices
			// return invalid graphics modes when any color level is requested, and in
			// those cases, the only way to get a valid mode is to not specify anything,
			// even requesting a default value of 0 would return an invalid mode.
			try
			{
				Log.Verbose("TexturedCube", "Loading with no Android settings");
				GraphicsMode = new AndroidGraphicsMode(0, 4, 0, 0, 0, false);

				// if you don't call this, the context won't be created
				base.CreateFrameBuffer();
				return;
			}
			catch (Exception ex)
			{
				Log.Verbose("TexturedCube", "{0}", ex);
			}
			throw new Exception("Can't load egl, aborting");
		}
#endif

		private void LoadTexture(int tex_id, byte[] texture)
		{
			GL.BindTexture(TextureTarget.Texture2D, tex_id);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.NearestMipmapLinear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

			//
			// 画像のロード＆RGBA色空間への変換＆OpenGLへの適用は、デバイス依存の処理になる
#if __ANDROID__
			using (var ms = new MemoryStream(texture))
			{
				var b = BitmapFactory.DecodeStream(ms);
				//
				// GLUTの関数を使って、良しなに設定する
				Android.Opengl.GLUtils.TexImage2D((int)All.Texture2D, 0, b, 0);

				b.Recycle();
			}
#endif

#if __IOS__
			var b = UIImage.LoadFromData(NSData.FromArray(texture));
			CGImage cgimage = b.CGImage;

			using (var dataProvider = cgimage.DataProvider)
			using (var data = dataProvider.CopyData())
			{
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
								(int)cgimage.Width, (int)cgimage.Height, 0,
								PixelFormat.Rgba, PixelType.UnsignedByte,
								data.Bytes);
			}
#endif
			GL.GenerateMipmap(TextureTarget.Texture2D);
		}

		private void SetupProjection()
		{
			if (Size.Width <= 0 || Size.Height <= 0)
				return;

			//
			// 水平回転はモデルに適用
			// (初期回転 X軸に-90度)
			Matrix4 model = Matrix4.Mult(Matrix4.CreateRotationX(MathHelper.DegreesToRadians(-90)), 
											Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_viewAngleX)));

			//
			// 垂直回転はビューに適用
			float dirV = (float)MathHelper.DegreesToRadians(_viewAngleY);
			float camZ = (float)Math.Cos(dirV) * 100.0f;
			float camY = (float)Math.Sin(dirV) * 100.0f;

			view = Matrix4.Mult(model, Matrix4.LookAt(new Vector3(0, 0, 0), new Vector3(0, camY, camZ), new Vector3(0, 1, 0)));

			GL.Viewport(0, 0, Size.Width, Size.Height);

			float aspect = (float)Size.Width / (float)Size.Height;
			projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(_fovy), aspect, 0.1f, 200.0f);
			projection = Matrix4.Mult(view, projection);
		}

		private void InitModel()
		{
			//
			// Vertex Buffer Objectに、Interleave化した配列データ(Vertex + TexCooord)を渡す
			//
			// なぜInterleave化する必要があるかは下記を参照
			// https://docs.google.com/document/pub?id=1DyW4bu-ni8cr28lnltu6_r-bhve44BdIwqb2ZjwpcrI
			//
			GL.GenBuffers(1, out _vbo);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)Sphere.GetInterleavedVertexSize(), Sphere.GetInterleavedVertices(), BufferUsage.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			//
			// インデックスバッファオブジェクトにインデックス配列データを渡す
			GL.GenBuffers(1, out _vbi);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _vbi);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)Sphere.GetIndexSize(), Sphere.GetIndices(), BufferUsage.StaticDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		private void DrawModel()
		{
			//
			// テクスチャをGPUに流す
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, _textureId);
			GL.Uniform1(_uniformTexture, 0);

			//
			// 頂点情報をバーテックスバッファオブジェクト経由でGPUに渡す
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
			GL.VertexAttribPointer(_attributeVertex, 3, VertexAttribPointerType.Float, 
									false, Sphere.GetInterleavedVertexStride(), 
									IntPtr.Zero);
			GL.EnableVertexAttribArray(_attributeVertex);

			//
			// テクスチャ座標情報をバーテックスバッファオブジェクト経由でGPUに渡す
			GL.VertexAttribPointer(_attributeTextcoord, 3, VertexAttribPointerType.Float, 
									false, Sphere.GetInterleavedVertexStride(), 
									new IntPtr(Sphere.GetInterleavedVertexTexCoordOffset()));
			GL.EnableVertexAttribArray(_attributeTextcoord);

			//
			// インデックスバッファを有効にして、球体をレンダリングする
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _vbi);
			GL.DrawElements(BeginMode.Triangles, (int)Sphere.GetIndexCount(), DrawElementsType.UnsignedInt, IntPtr.Zero);

			//
			// 無効にする
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		private void RenderSphere()
		{
			MakeCurrent();

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			GL.ClearColor(0, 0, 0, 1);

			GL.UseProgram(_program);

			GL.UniformMatrix4(_uniformProjection, false, ref projection);

			DrawModel();

			SwapBuffers();
		}

		private bool LoadShaders(string vertShaderSource, string fragShaderSource, out int program)
		{
			int vertShader, fragShader;

			program = GL.CreateProgram();

			if (!CompileShader(ShaderType.VertexShader, vertShaderSource, out vertShader))
			{
				Console.WriteLine("Failed to compile vertex shader");
				return false;
			}

			if (!CompileShader(ShaderType.FragmentShader, fragShaderSource, out fragShader))
			{
				Console.WriteLine("Failed to compile fragment shader");
				return false;
			}

			GL.AttachShader(program, vertShader);
			GL.AttachShader(program, fragShader);

			GL.BindAttribLocation(program, _attributeVertex, "position");
			GL.BindAttribLocation(program, _attributeTextcoord, "texcoord");

			if (!LinkProgram(program))
			{
				Console.WriteLine("Failed to link program: {0:x}", program);

				if (vertShader != 0)
					GL.DeleteShader(vertShader);

				if (fragShader != 0)
					GL.DeleteShader(fragShader);

				if (program != 0)
				{
					GL.DeleteProgram(program);
					program = 0;
				}
				return false;
			}

			_uniformProjection = GL.GetUniformLocation(program, "projection");
			_uniformTexture = GL.GetUniformLocation(program, "text");

			if (vertShader != 0)
			{
				GL.DetachShader(program, vertShader);
				GL.DeleteShader(vertShader);
			}

			if (fragShader != 0)
			{
				GL.DetachShader(program, fragShader);
				GL.DeleteShader(fragShader);
			}

			return true;
		}

		private bool CompileShader(ShaderType type, string src, out int shader)
		{
			shader = GL.CreateShader(type);
			GL.ShaderSource(shader, src);
			GL.CompileShader(shader);

			int logLength = 0;
			GL.GetShader(shader, ShaderParameter.InfoLogLength, out logLength);
			if (logLength > 0)
			{
				Console.WriteLine("Shader compile log:\n{0}", GL.GetShaderInfoLog(shader));
			}

			int status = 0;
			GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
			if (status == 0)
			{
				GL.DeleteShader(shader);
				return false;
			}

			return true;
		}

		private bool LinkProgram(int prog)
		{
			GL.LinkProgram(prog);

			int logLength = 0;
			GL.GetProgram(prog, ProgramParameter.InfoLogLength, out logLength);
			if (logLength > 0)
				Console.WriteLine("Program link log:\n{0}", GL.GetProgramInfoLog(prog));

			int status = 0;
			GL.GetProgram(prog, ProgramParameter.LinkStatus, out status);
			if (status == 0)
				return false;

			return true;
		}

		//
		// カスタムレンダラーにすると、ユーザアクションが、.NetStandard側では受け取れなくなるので
		// ネイティブコントロール側でユーザアクションを実装する必要がある
		//
#if __ANDROID__
		public override bool OnTouchEvent(MotionEvent e)
		{
			base.OnTouchEvent(e);

			if (e.Action == MotionEventActions.Down)
			{
				prevx = e.GetX();
				prevy = e.GetY();
			}

			if (e.Action == MotionEventActions.Move)
			{
				float e_x = e.GetX();
				float e_y = e.GetY();

				float xdiff = (prevx - e_x);
				float ydiff = (prevy - e_y);

				_viewAngleX += 90.0f * xdiff / (float)Size.Width;
				_viewAngleY -= 90.0f * ydiff / (float)Size.Height;

				while (_viewAngleX >= 360) _viewAngleX -= 360;
				while (_viewAngleX < 0) _viewAngleX += 360;

				_viewAngleY = Math.Max(-80, _viewAngleY);
				_viewAngleY = Math.Min(80, _viewAngleY);

				prevx = e_x;
				prevy = e_y;

				SetupProjection();
				RenderSphere();
			}

			return true;
		}
#endif

#if __IOS__
		public override void TouchesBegan(NSSet touches, UIEvent e)
		{
			var touch = (UITouch)e.TouchesForView(this).AnyObject;
			CGPoint pt = touch.LocationInView(this);
			prevx = (float)pt.X;
			prevy = (float)pt.Y;
		}

		public override void TouchesMoved(NSSet touches, UIEvent e)
		{
			var touch = (UITouch)e.TouchesForView(this).AnyObject;

			CGPoint pt = touch.LocationInView(this);
			float e_x = (float)pt.X;
			float e_y = (float)pt.Y;

			float xdiff = (prevx - e_x);
			float ydiff = (prevy - e_y);

			_viewAngleX += 90.0f * xdiff / (float)Size.Width;
			_viewAngleY -= 90.0f * ydiff / (float)Size.Height;

			while (_viewAngleX >= 360) _viewAngleX -= 360;
			while (_viewAngleX < 0) _viewAngleX += 360;

			_viewAngleY = Math.Max(-80, _viewAngleY);
			_viewAngleY = Math.Min(80, _viewAngleY);

			prevx = e_x;
			prevy = e_y;

			SetupProjection();
			RenderSphere();
		}

		public override void TouchesEnded(NSSet touches, UIEvent e)
		{
		}
#endif
	}
}
