using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Helpers;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;

namespace PeliculasAPI.Controllers
{
    public class CustomBaseController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public CustomBaseController(ApplicationDbContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        protected async Task<List<TDTO>> Get<TEntidad, TDTO>() where TEntidad : class
        {
            var entidades = await context.Set<TEntidad>().AsNoTracking().ToListAsync();
            var dtos = mapper.Map<List<TDTO>>(entidades);
            return dtos;
        }

        protected async Task<List<TDTO>> Get<TEntity, TDTO>(FiltroDTO filtroDTO)
            where TEntity : class, new()
        {
            var queryable = context.Set<TEntity>().AsQueryable();

            if (!string.IsNullOrEmpty(filtroDTO.Filtro))
            {
                var filtros = JsonConvert.DeserializeObject<JObject>(filtroDTO.Filtro);
                var objeto = new TEntity();
               
                foreach (var f in filtros)
                {
                    var llaves = f.Key.ToString().Split('.');
                    var nestedProperty = GetNestedProperty(objeto, llaves);

                    if (nestedProperty != null)
                    {
                        var query = nestedProperty.PropertyType == typeof(string) ?
                            $"{f.Key}.Contains(@0)" :
                            $"{f.Key} == @0";

                        queryable = queryable.Where(query, f.Value.ToString());
                    }

                }
            }

            var count = await queryable.CountAsync();

            if (!string.IsNullOrEmpty(filtroDTO.Sort))
            {
                var sortVal = JsonConvert.DeserializeObject<List<string>>(filtroDTO.Sort);
                var condicion = sortVal.First();
                var orden = sortVal.Last() == "ASC" ? "" : "descending";
                queryable = queryable.OrderBy($"{condicion} {orden}");
            }

            var desde = 0;
            var hasta = 0;
            if (!string.IsNullOrEmpty(filtroDTO.Rango))
            {
                var rangeVal = JsonConvert.DeserializeObject<List<int>>(filtroDTO.Rango);
                desde = rangeVal.First();
                hasta = rangeVal.Last();
                queryable = queryable.Skip(desde).Take(hasta - desde + 1);
            }

             var entityTypeName = typeof(TEntity).Name.ToLower();
             HttpContext.InsertarParametrosFiltros(entityTypeName, desde, hasta, count);
            return mapper.Map<List<TDTO>>(queryable);


        }

        protected async Task<List<TDTO>> Get<TEntidad, TDTO>(PaginacionDTO paginacionDTO) where TEntidad : class
        {
            var queryable = context.Set<TEntidad>().AsQueryable();
            return await Get<TEntidad, TDTO>(paginacionDTO, queryable);
        }

        protected async Task<List<TDTO>> Get<TEntidad, TDTO>(PaginacionDTO paginacionDTO,
            IQueryable<TEntidad> queryable)
            where TEntidad : class
        {
            await HttpContext.InsertarParametrosPaginacion(queryable, paginacionDTO.CantidadRegistrosPorPagina);
            var entidades = await queryable.Paginar(paginacionDTO).ToListAsync();
            return mapper.Map<List<TDTO>>(entidades);
        }

        protected async Task<ActionResult<TDTO>> Get<TEntidad, TDTO>(int id) where TEntidad : class, IId
        {
            var entidad = await context.Set<TEntidad>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

            if (entidad == null)
            {
                return NotFound();
            }

            return mapper.Map<TDTO>(entidad);
        }

        protected async Task<ActionResult> Post<TCreacion, TEntidad, TLectura>(TCreacion creacionDTO, string nombreRuta) where TEntidad : class, IId
        {
            var entidad = mapper.Map<TEntidad>(creacionDTO);
            context.Add(entidad);
            await context.SaveChangesAsync();
            var dtoLectura = mapper.Map<TLectura>(entidad);

            return new CreatedAtRouteResult(nombreRuta, new { id = entidad.Id }, dtoLectura);
        }

        protected async Task<ActionResult> Put<TCreacion, TEntidad>(int id, TCreacion creacionDTO) where TEntidad : class, IId
        {
            var entidad = mapper.Map<TEntidad>(creacionDTO);
            entidad.Id = id;
            context.Entry(entidad).State = EntityState.Modified;
            await context.SaveChangesAsync();
            return NoContent();
        }

        protected async Task<ActionResult> Patch<TEntidad, TDTO>(int id, JsonPatchDocument<TDTO> patchDocument) where TDTO : class where TEntidad: class, IId
        {
            if (patchDocument == null)
            {
                return BadRequest();
            }

            var entidadDB = await context.Set<TEntidad>().FirstOrDefaultAsync(x => x.Id == id);

            if (entidadDB == null)
            {
                return NotFound();
            }

            var entidadDTO = mapper.Map<TDTO>(entidadDB);

            patchDocument.ApplyTo(entidadDTO, ModelState);

            var esValido = TryValidateModel(entidadDTO);

            if (!esValido)
            {
                return BadRequest(ModelState);
            }

            mapper.Map(entidadDTO, entidadDB);

            await context.SaveChangesAsync();

            return NoContent();
        }

        protected async Task<ActionResult> Delete<TEntidad>(int id) where TEntidad: class, IId, new()
        {
            var existe = await context.Set<TEntidad>().AnyAsync(x => x.Id == id);

            if (!existe)
            {
                return NotFound();
            }

            context.Remove(new TEntidad() { Id = id });
            await context.SaveChangesAsync();

            return NoContent();
        }
        private PropertyInfo GetNestedProperty(object objeto, string[] properties)
        {
            PropertyInfo property = null;
            foreach (var propertyName in properties)
            {
                if (objeto == null) return null;
                property = objeto.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                objeto = property.GetValue(objeto, null);
            }

            return property;
        }

    }
}
