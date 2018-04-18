using System;
using System.Linq;
using Foundation;
using WebKit;
using Xam.Plugin.WebView.Abstractions;
using UIKit;

namespace Xam.Plugin.WebView.iOS
{
    public class FormsNavigationDelegate : WKNavigationDelegate
    {

        readonly WeakReference<FormsWebViewRenderer> Reference;

        public FormsNavigationDelegate(FormsWebViewRenderer renderer)
        {
            Reference = new WeakReference<FormsWebViewRenderer>(renderer);
        }

        public bool AttemptOpenCustomUrlScheme(NSUrl url)
        {
            var app = UIApplication.SharedApplication;

            if (app.CanOpenUrl(url))
                return app.OpenUrl(url);

            return false;
        }

        [Export("webView:decidePolicyForNavigationAction:decisionHandler:")]
        public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
        {
			if (Reference == null || !Reference.TryGetTarget(out FormsWebViewRenderer renderer)) return;
			if (renderer.Element == null) return;
            
            var response = renderer.Element.HandleNavigationStartRequest(navigationAction.Request.Url.ToString());
            
            if (response.Cancel || response.OffloadOntoDevice)
            {
                if (response.OffloadOntoDevice)
                    AttemptOpenCustomUrlScheme(navigationAction.Request.Url);

                decisionHandler(WKNavigationActionPolicy.Cancel);
            }

            else
            {
                decisionHandler(WKNavigationActionPolicy.Allow);
                renderer.Element.Navigating = true;
            }
        }

        public override void DecidePolicy(WKWebView webView, WKNavigationResponse navigationResponse, Action<WKNavigationResponsePolicy> decisionHandler)
        {
			if (Reference == null || !Reference.TryGetTarget(out FormsWebViewRenderer renderer)) return;
			if (renderer.Element == null) return;

            if (navigationResponse.Response is NSHttpUrlResponse) {
                var code = ((NSHttpUrlResponse)navigationResponse.Response).StatusCode;
                if (code >= 400) {
                    renderer.Element.Navigating = false;
                    renderer.Element.HandleNavigationError((int) code);
                    decisionHandler(WKNavigationResponsePolicy.Cancel);
                    return;
                }
            }

            decisionHandler(WKNavigationResponsePolicy.Allow);
        }

        [Export("webView:didFinishNavigation:")]
        public async override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        {         
			FormsWebViewRenderer renderer;
			if (Reference == null || !Reference.TryGetTarget(out renderer)) return;
			if (renderer.Element == null) return;

            renderer.Element.HandleNavigationCompleted(webView.Url.ToString());
            await renderer.OnJavascriptInjectionRequest(FormsWebView.InjectedFunction);
            
			if (Reference == null || !Reference.TryGetTarget(out renderer)) return;
            if (renderer.Element == null) return;

            if (renderer.Element.EnableGlobalCallbacks)
			{
				string[] globals = FormsWebView.GlobalRegisteredCallbacks.Keys.ToArray();
				for (int i = 0; i < globals.Length; i++)
				{
					string f = globals[i];
					await renderer.OnJavascriptInjectionRequest(FormsWebView.GenerateFunctionScript(f));

					if (Reference == null || !Reference.TryGetTarget(out renderer)) return;
                    if (renderer.Element == null) return;
				}
			}

			string[] locals = renderer.Element.LocalRegisteredCallbacks.Keys.ToArray();
			for (int i = 0; i < locals.Length; i++)
			{
				string f = locals[i];
                await renderer.OnJavascriptInjectionRequest(FormsWebView.GenerateFunctionScript(f));

				if (Reference == null || !Reference.TryGetTarget(out renderer)) return;
                if (renderer.Element == null) return;
			}

            renderer.Element.CanGoBack = webView.CanGoBack;
            renderer.Element.CanGoForward = webView.CanGoForward;
            renderer.Element.Navigating = false;
            renderer.Element.HandleContentLoaded();
        }
    }
}
