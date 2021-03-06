﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APIGestor.Business;
using APIGestor.Models;

namespace APIGestor.Controllers {
    [Route("api/projeto/")]
    [ApiController]
    [Authorize("Bearer")]
    public class ResultadoEconomicoController : ControllerBase {
        private ResultadoEconomicoService _service;

        public ResultadoEconomicoController( ResultadoEconomicoService service ) {
            _service = service;
        }

        [HttpGet("{projetoId}/ResultadoEconomico")]
        public IEnumerable<ResultadoEconomico> Get( int projetoId ) {
            return _service.ListarTodos(projetoId);
        }

        [HttpGet("ResultadoEconomico/{Id}")]
        public ActionResult<ResultadoEconomico> GetA( int id ) {
            var ResultadoEconomico = _service.Obter(id);
            if(ResultadoEconomico != null)
                return ResultadoEconomico;
            else
                return NotFound();
        }

        [Route("[controller]")]
        [HttpPost]
        public ActionResult<Resultado> Post( [FromBody]ResultadoEconomico ResultadoEconomico ) {

            if(_service.UserProjectCan(ResultadoEconomico.ProjetoId, User, Authorizations.ProjectPermissions.LeituraEscrita)) {
                var resultado = _service.Incluir(ResultadoEconomico);
                if(resultado.Sucesso) {
                    this.CreateLog(_service, ResultadoEconomico.ProjetoId, ResultadoEconomico);
                }
                return resultado;
            }
            return Forbid();

        }

        [Route("[controller]")]
        [HttpPut]
        public ActionResult<Resultado> Put( [FromBody]ResultadoEconomico ResultadoEconomico ) {
            var Resultado = _service._context.ResultadosEconomico.Find(ResultadoEconomico.Id);
            if(_service.UserProjectCan(Resultado.ProjetoId, User, Authorizations.ProjectPermissions.LeituraEscrita)) {
                _service._context.Entry(Resultado).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                var resultado = _service.Atualizar(ResultadoEconomico);
                if(resultado.Sucesso) {
                    this.CreateLog(_service, Resultado.ProjetoId, ResultadoEconomico, Resultado);
                }


                return resultado;
            }
            return Forbid();
        }

        [HttpDelete("[controller]/{Id}")]
        public ActionResult<Resultado> Delete( int id ) {
            var Resultado = _service.Obter(id);
            if(_service.UserProjectCan(Resultado.ProjetoId, User, Authorizations.ProjectPermissions.Administrator)) {
                var resultado = _service.Excluir(id);
                if(resultado.Sucesso) {
                    this.CreateLog(_service, Resultado.ProjetoId, Resultado);
                }
                return resultado;
            }
            return Forbid();
        }
    }
}