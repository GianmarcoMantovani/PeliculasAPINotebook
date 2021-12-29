using Microsoft.EntityFrameworkCore;

namespace PeliculasAPI.Helpers
{
    public static class HttpContextExtensions
    {
        public async static Task InsertarParametrosPaginacion<T>(this HttpContext httpContext,
            IQueryable<T> queryable, int cantidadRegistrosPorPagina)
        {
            double cantidad = await queryable.CountAsync();
            double cantidadPaginas = Math.Ceiling(cantidad / cantidadRegistrosPorPagina);
            httpContext.Response.Headers.Add("cantidadPaginas", cantidadPaginas.ToString());
        }
        public async static void InsertarParametrosFiltros(this HttpContext httpContext, string entityType, int rangeStart, int rangeEnd, int recordsAmount)
        {
            httpContext.Response.Headers.Add("Access-Control-Expose-Headers", "Content-Range");
            httpContext.Response.Headers.Add("Content-Range", $"{entityType} {rangeStart}-{rangeEnd}/{recordsAmount}");
        }
    }
}
