using Flurl;

namespace PolygonIo.WebApi
{
    static class UrlHelpers
    {
        public static Url SetQueryParamIfNotNull<T>(this Url url, string name, T t)
        {
            if (t != null)
                url.SetQueryParam(name, t);

            return url;
        }
    }
}
