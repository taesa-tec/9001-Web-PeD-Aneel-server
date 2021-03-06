﻿using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APIGestor.Business;
using APIGestor.Models;

namespace APIGestor.Controllers {
    [Route("api/projeto/")]
    [ApiController]
    [Authorize("Bearer")]
    public class AlocacaoRhsController : ControllerBase {
        private AlocacaoRhService _service;

        public AlocacaoRhsController( AlocacaoRhService service ) {
            _service = service;
        }

        [HttpGet("{projetoId}/AlocacaoRhs")]
        public IEnumerable<AlocacaoRh> Get( int projetoId ) {

            return _service.ListarTodos(projetoId);
        }

        [Route("[controller]")]
        [HttpPost]
        public ActionResult<Resultado> Post( [FromBody]AlocacaoRh AlocacaoRh ) {
            if(_service.UserProjectCan((int)AlocacaoRh.ProjetoId, User, Authorizations.ProjectPermissions.LeituraEscrita)) {
                var resultado = _service.Incluir(AlocacaoRh);
                if(resultado.Sucesso) {
                    this.CreateLog(_service, (int)AlocacaoRh.ProjetoId, _service.Obter(AlocacaoRh.Id));
                }
                return resultado;
            }
            return Forbid();
        }

        [Route("[controller]")]
        [HttpPut]
        public ActionResult<Resultado> Put( [FromBody]AlocacaoRh AlocacaoRh ) {
            if(_service.UserProjectCan((int)AlocacaoRh.ProjetoId, User, Authorizations.ProjectPermissions.LeituraEscrita)) {
                var oldAlocacao = _service.Obter(AlocacaoRh.Id);
                _service._context.Entry(oldAlocacao).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                var resultado = _service.Atualizar(AlocacaoRh);
                if(resultado.Sucesso) {
                    this.CreateLog(_service, (int)AlocacaoRh.ProjetoId, _service.Obter(AlocacaoRh.Id), oldAlocacao);
                }

                return resultado;
            }
            return Forbid();
        }

        [HttpDelete("[controller]/{Id}")]
        public ActionResult<Resultado> Delete( int id ) {
            var alocacao = _service.Obter(id);
            if(alocacao != null) {
                if(_service.UserProjectCan((int)alocacao.ProjetoId, User, Authorizations.ProjectPermissions.Administrator)) {
                    var resultado = _service.Excluir(id);
                    if(resultado.Sucesso) {
                        this.CreateLog(_service, (int)alocacao.ProjetoId, alocacao);
                    }
                    return resultado;
                }
                return Forbid();
            }
            return NotFound();
        }
    }
}