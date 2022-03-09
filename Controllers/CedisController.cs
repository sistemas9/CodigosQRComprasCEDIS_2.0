using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodigosQRComprasCEDIS_2._0.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodigosQRComprasCEDIS_2._0.Controllers
{
    public class CedisController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SubrevisionLinea()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> GetDataOCAsync(String OC, String Revision, String Company)
        {
            CapturadeOC capturadeOC = new CapturadeOC(Company);
            ResponseOrdenCompra oc = await capturadeOC.GetDataOC(OC);
            Cedis cedisLine = new Cedis(Company);
            List<String[]> ocLine = await cedisLine.GetDataOCLinea(OC, Revision);
            return Json(new { data = ocLine, dataocHead = (oc.value.Count > 0) ? oc.value[0] : null });
        }

        [HttpPost]
        public async Task<ActionResult> UploadFilesAsync(String purchaseOrderNumber,String revision, String purchaseOrderNumberRev,String Company)
        {
            IFormFileCollection files = Request.Form.Files;
            var fail = false;
            ResultImg result = new ResultImg();
            foreach (IFormFile file in files)
            //for(int i = 0; i < files.Count; i++)  
            {
                Cedis cedisImages = new Cedis(Company);
                result = await cedisImages.uploadFiles(file, purchaseOrderNumber, revision, purchaseOrderNumberRev, User.Identity.Name);
                if (result.estatus == "FailDuplicado")
                {
                    fail = true;
                }
                Thread.Sleep(250);
            }
            if (fail)
            {
                return Ok("Imagen duplicada");
            }
            else
            {
                return Ok(result);
            }
        }

        [HttpPost]
        public async Task<ActionResult> GetServerDocsAsync(String purchaseOrderNumber, String lineNumber, String itemNumber,String Company)
        {
            Cedis cedisImgs = new Cedis(Company);
            var result = await cedisImgs.getServerDocs(purchaseOrderNumber, lineNumber, itemNumber);
            return Ok(new { mockfiles = result });
        }

        [HttpPost]
        public async Task<ActionResult> RemoveFilesAsync(String purchaseOrderNumber, String lineNumber, String itemNumber, int idCodigos,String Company)
        {
            Cedis cedisRemoveImage = new Cedis(Company);
            var result = await cedisRemoveImage.removeFiles(purchaseOrderNumber, lineNumber, itemNumber, idCodigos);
            return Ok(result);
        }

        public async Task<ActionResult> GetListOCAsync(String Company)
        {
            Cedis cedisOC = new Cedis(Company);
            List<OCList> ListaOc = await cedisOC.getListOC();
            return Json(new { data = ListaOc });
        }

        public async Task<JsonResult> SendMailComprasAsync(String oc, String revision,String company)
        {
            Cedis captura = new Cedis(company);
            var result = await captura.SendMailCompras(oc, revision,company);
            return Json(result);
        }

        public async Task<JsonResult> SetSubrevisionAsync(List<SubRevision> subRev, String revision,String Company)
        {
            Cedis cedisSubRev = new Cedis(Company);
            var result = await cedisSubRev.SetSubrevision(subRev,revision);
            return Json(result);
        }
    }
}