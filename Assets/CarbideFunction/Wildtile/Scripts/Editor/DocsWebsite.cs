using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class DocsWebsite
    {
        public static string DocumentationWebsiteRoot
        {
            get {
                EnsureDocumentationWebsiteRootLoaded();
                return documentationWebsiteRoot;
            }
        }

        private static string documentationWebsiteRoot;

        private static void EnsureDocumentationWebsiteRootLoaded()
        {
            if (documentationWebsiteRoot == null)
            {
                documentationWebsiteRoot = LoadDocumentationWebsiteRoot();
            }
        }

        private const string documentationLinkAssetGuid = "60fe5f483b46e564997c409a0865b0dd";
        private const string urlHeader = "url=";
        private const string urlTail = "\"";
        private static string LoadDocumentationWebsiteRoot()
        {
            var documentationLinkAssetUrl = AssetDatabase.GUIDToAssetPath(documentationLinkAssetGuid);
            var loadedRedirectWebsite = AssetDatabase.LoadAssetAtPath<TextAsset>(documentationLinkAssetUrl);
            var headerIndex = loadedRedirectWebsite.text.IndexOf(urlHeader);

            Assert.IsTrue(headerIndex > 0);

            var urlStartIndex = headerIndex + urlHeader.Length;
            var tailIndex = loadedRedirectWebsite.text.IndexOf(urlTail, urlStartIndex);

            Assert.IsTrue(tailIndex > 0);

            var url = loadedRedirectWebsite.text.Substring(urlStartIndex, tailIndex - urlStartIndex);

            return url;
        }
    }
}
