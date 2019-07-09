﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APIGestor.Business;
using APIGestor.Models;
using System.IdentityModel.Tokens.Jwt;

namespace APIGestor.Controllers
{
    [Route("api/projeto/")]
    [ApiController]
    [Authorize("Bearer")]
    public class RegistroFinanceiroController : ControllerBase
    {
        private RegistroFinanceiroService _service;

        public RegistroFinanceiroController(RegistroFinanceiroService service)
        {
            _service = service;
        }

        // [HttpGet("{projetoId}/RegistroFinanceiro")]
        // public IEnumerable<RegistroFinanceiro> Get(int projetoId)
        // {
        //     return _service.ListarTodos(projetoId);
        // }

        [Route("[controller]")]
        [HttpPost]
        public ActionResult<Resultado> Post([FromBody]RegistroFinanceiro RegistroFinanceiro)
        {
            var userId = User.FindFirst(JwtRegisteredClaimNames.Jti).Value;
            return _service.Incluir(RegistroFinanceiro, userId);
        }

        [Route("[controller]")]
        [HttpPut]
        public ActionResult<Resultado> Put([FromBody]RegistroFinanceiro RegistroFinanceiro)
        {
            var userId = User.FindFirst(JwtRegisteredClaimNames.Jti).Value;
            return _service.Atualizar(RegistroFinanceiro, userId);
        }

        [HttpDelete("[controller]/{Id}")]
        public ActionResult<Resultado> Delete(int id)
        {
            return _service.Excluir(id);
        }

        [HttpGet("{projetoId}/RegistroFinanceiro/{status}")]
        public IEnumerable<RegistroFinanceiro> Get(int projetoId, StatusRegistro status)
        {
            return _service.ListarTodos(projetoId, status);
        }
        
        // [HttpGet("{projetoId}/RegistroFinanceiro/exportar")]
        // public FileResult Download(int id)  
        // {  
        //     var registro = _service.Obter(id);
        //     if (registro==null)
        //         return null;
        //     byte[] fileBytes = System.IO.File.ReadAllBytes(@upload.Url+id);
        //     string fileName = upload.NomeArquivo;
        //     return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        // }
    }
}