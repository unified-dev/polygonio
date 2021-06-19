using Flurl;

namespace PolygonIo.WebApi
{
    static class UrlHelpers
    {
        public static Url SetQueryParamIfNotNull<T>(this Url url, string name, T t, bool makeLowerCase = true)
        {
            if (t != null)
                url.SetQueryParam(name, makeLowerCase ? t.ToString().ToLowerInvariant() : t);

            return url;
        }
    }
}
