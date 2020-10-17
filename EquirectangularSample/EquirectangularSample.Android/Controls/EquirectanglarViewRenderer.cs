using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using EquirectangularSample.Controls;
using EquirectangularSample.Droid.Controls;
using Shared;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(EquirectanglarView), typeof(EquirectanglarViewRenderer))]

namespace EquirectangularSample.Droid.Controls
{
    public class EquirectanglarViewRenderer : ViewRenderer
    {
        private Android.Widget.RelativeLayout baseView;

        private OGLView oglView;

        public EquirectanglarViewRenderer(Context context) : base(context)
        {

        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.View> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                ((EquirectanglarView)e.OldElement).ElementSizeChanged -= ElementSizeChanged;
            }

            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    //
                    // ViewGroupを一つかませて、回転時にOGLViewを再作成して対応する
                    baseView = new Android.Widget.RelativeLayout(Context);
                    SetNativeControl(baseView);

                    ((EquirectanglarView)e.NewElement).ElementSizeChanged += ElementSizeChanged;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public void ElementSizeChanged(double w, double h)
        {
            if (oglView != null)
            {
                //
                // 現在のOGLViewを削除
                baseView.RemoveView(oglView);
                oglView.Dispose();
                oglView = null;
            }

            oglView = new OGLView(this.Context)
            {
                Sphere = ((EquirectanglarView)Element).Sphere,
                VertexShader = ((EquirectanglarView)Element).VertexShader,
                FragmentShader = ((EquirectanglarView)Element).FragmentShader,
                TextureImage = ((EquirectanglarView)Element).TextureImage
            };
            baseView.AddView(oglView, LayoutParams.MatchParent);
        }
    }
}