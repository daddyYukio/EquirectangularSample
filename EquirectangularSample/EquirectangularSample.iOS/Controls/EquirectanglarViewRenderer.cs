using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CoreGraphics;
using EquirectangularSample.Controls;
using EquirectangularSample.iOS.Controls;
using Foundation;
using Shared;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(EquirectanglarView), typeof(EquirectanglarViewRenderer))]

namespace EquirectangularSample.iOS.Controls
{
    public class EquirectanglarViewRenderer : ViewRenderer
    {
        private UIView baseView;

        private OGLView oglView;

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
                    // UIViewを一つかませて、回転時にOGLViewを再作成して対応する
                    baseView = new UIView();
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
                oglView.RemoveFromSuperview();
                oglView.Dispose();
                oglView = null;
            }

            oglView = new OGLView(new CGRect(0, 0, w, h))
            {
                Sphere = ((EquirectanglarView)Element).Sphere,
                VertexShader = ((EquirectanglarView)Element).VertexShader,
                FragmentShader = ((EquirectanglarView)Element).FragmentShader,
                TextureImage = ((EquirectanglarView)Element).TextureImage
            };
            baseView.Add(oglView);
            //
            // iOSでは　iPhoneOSGameView::OnLoadがコールされないので、ここで明示的にInitializeをコールする
            //
            oglView.Initialize();
        }

    }
}