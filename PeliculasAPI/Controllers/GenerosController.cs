using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using System.Collections.Generic;

namespace PeliculasAPI.Controllers
{
    /// <summary>
    /// Manejar generos de las peliculas
    /// </summary>
    [ApiController]
    [Route("api/generos")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, Moderador")]
    public class GenerosController : CustomBaseController //Hereda
    {

        public GenerosController(ApplicationDbContext context, 
            IMapper mapper) //Inyectamos appdbcont para acceso a EF
            :base (context, mapper)
        {
        }
        /// <summary>
        /// Obtener todos los genereos de peliculas
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<GeneroDTO>>> Get()
        {
            return await Get<Genero, GeneroDTO> ();
        }
        /// <summary>
        /// obtener generos de peliculas por ID
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id:int}", Name = "obtenerGenero")]
        [AllowAnonymous]
        public async Task<ActionResult<GeneroDTO>> Get(int id)
        {
            return await Get<Genero, GeneroDTO> (id);
        }
        /// <summary>
        /// Crear un nuevo genero para una pelicula
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] GeneroCreacionDTO generoCreacionDTO)
        {
            return await Post<GeneroCreacionDTO, Genero, GeneroDTO>(generoCreacionDTO, "obtenerGenero");
        }
        /// <summary>
        /// Hacer cambios a un genero
        /// </summary>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromBody] GeneroCreacionDTO generoCreacionDTO)
        {
            return await Put<GeneroCreacionDTO, Genero>(id, generoCreacionDTO);
        }
        /// <summary>
        /// Eliminar un genero
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            return await Delete<Genero>(id);
        }
    }
}
