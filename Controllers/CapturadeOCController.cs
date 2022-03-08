using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodigosQRComprasCEDIS_2._0.Helpers;
using CodigosQRComprasCEDIS_2._0.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;
using System.Text;
using iText.Html2pdf;

namespace CodigosQRComprasCEDIS_2._0.Controllers
{
  public class CapturadeOCController : Controller
  {
    public IActionResult IndexAsync()
    {
      ViewBag.vista = "capturaOC";
      //LdapHelper ldap = new LdapHelper();
      //List<EntryAD> groups = ldap.SearchForGroup("OC");
      //ViewBag.AD = groups;
      return View();
    }

    public IActionResult VisorCodigosQR()
    {
      ViewBag.vista = "visor";
      return View();
    }

    public async Task<IActionResult> Carrusel(String purchaseOrderNumber, String Company)
    {
      ViewBag.vista = "carrusel";
      ResultTransform result = await GetImagesPathsAsync(purchaseOrderNumber, "0", "N/A", null, 0, Company);
      ViewBag.images = result.data;
      GetConfigsData configs = new GetConfigsData();
      String config = configs.GetConfigurationData("Config").Result;
      if (config != "DESARROLLO")
      {
        ViewBag.rutaImg = "https://ayt-apps.eastus.cloudapp.azure.com/CodigosQR";
      }
      else
      {
        //ViewBag.rutaImg = "https://localhost:6061/";
        ViewBag.rutaImg = "https://ayt-apps.eastus.cloudapp.azure.com/CodigosQRTest";
      }
      return View();
    }

    [HttpPost]
    public async Task<JsonResult> GetDataOCAsync(String OC, String Company)
    {
      CapturadeOC capturadeOC = new CapturadeOC(Company);
      ResponseOrdenCompra oc = await capturadeOC.GetDataOC(OC);
      List<String[]> ocLine = await capturadeOC.GetDataOCLinea(OC);
      return Json(new { data = ocLine, dataocHead = (oc.value.Count > 0) ? oc.value[0] : null });
    }

    [HttpPost]
    public async Task<JsonResult> GetDataOCCodigosAsync(String OC, String ocRevision, String revision, String Company)
    {
      CapturadeOC capturadeOC = new CapturadeOC(Company);
      ResponseOrdenCompra oc = await capturadeOC.GetDataOC(OC);
      List<String[]> ocLine = await capturadeOC.GetDataOCLineaCodigos(OC, ocRevision, revision);
      return Json(new { data = ocLine, dataocHead = (oc.value.Count > 0) ? oc.value[0] : null });
    }

    [HttpPost]
    public async Task<JsonResult> SaveDataOCAsync(OrdenCompraHeader header, List<OrdenCompraLinea> lines, String Company)
    {
      CapturadeOC capturadeOC = new CapturadeOC(Company);
      JsonResult response = Json(await capturadeOC.SaveDataOC(header, lines, User.Identity.Name));
      return response;
    }

    [HttpPost]
    public async Task<ResultTransform> GetImagesPathsAsync(String purchaseOrderNumber, String lineNumber, String itemNumber, String oc, int revision, String Company)
    {
      CapturadeOC capturadeOC = new CapturadeOC(Company);
      ResultTransform result = await capturadeOC.GetImagesPaths(purchaseOrderNumber, lineNumber, itemNumber, oc, revision);
      return result;
    }

    public async Task<JsonResult> SaveImagesComentAsync(List<Data> DataLote, List<ComentImg> ComentImagen, DataOrd DataOrden, String Revision, String Company)
    {
      CapturadeOC capturadeOC = new CapturadeOC(Company);
      var Usuario = User.Identity.Name;
      var result = await capturadeOC.SaveImagesComent(DataLote, ComentImagen, DataOrden, Usuario, Revision);
      return Json(result);
    }

    public async Task<JsonResult> RemoveLoteComentAsync(DataLoteComent data, String Company)
    {
      CapturadeOC capturadeOC = new CapturadeOC(Company);
      var result = await capturadeOC.RemoveLoteComent(data);
      return Json(result);
    }

    public async Task<JsonResult> CargarDataOCVisorAsync(String Company)
    {
      CapturadeOC capturadeOC = new CapturadeOC(Company);
      List<String[]> oc = await capturadeOC.GetDataOCVisor();
      return Json(new { data = oc });
    }

    public async Task<JsonResult> GetDataLineasVisorAsync(String OC, String itemNumber, String revision, String Company)
    {
      CapturadeOC capturadeOC = new CapturadeOC(Company);
      List<String[]> ocLine = await capturadeOC.GetDataOCLineaVisor(OC, itemNumber, revision);
      return Json(new { data = ocLine });
    }

    public async Task<FileResult> ConvertToPDF(String html, String Company)
    {
      PdfTableLinesTransform res = Newtonsoft.Json.JsonConvert.DeserializeObject<PdfTableLinesTransform>(html);
      var arrayCompleto = res.data;
      CapturadeOC capturadeOC = new CapturadeOC(Company);
      String PDF = await capturadeOC.PDFVisor(arrayCompleto);
      byte[] pdfByteArray = Encoding.UTF8.GetBytes(PDF);
      //Console.WriteLine(Encoding.ASCII.GetChars(pdfByteArray));

      var pdfstream = new MemoryStream();

      var w = new StreamWriter(pdfstream);
      w.Write(PDF);
      w.Flush();
      pdfstream.Position = 0;
      HtmlConverter.ConvertToPdf(PDF, pdfstream);
      MemoryStream pdfstream2 = new MemoryStream(pdfstream.ToArray());
      FileResult fileDownload = File(pdfstream2, "application/pdf");
      return fileDownload;

    }

    public IActionResult EnviarPDF(String ocPDF, String revisionPDF, String ocLotePDF, String Company)
    {
      CapturadeOC capturadeOC = new CapturadeOC(Company);
      var result = capturadeOC.SendMail(ocPDF, revisionPDF, ocLotePDF);
      return Json(result);
    }

    public IActionResult SetRevision(String oc)
    {
      Revisiones.SetRevision(oc);
      return Json("Exito");
    }

    public async Task<ActionResult> GetListOCAsync(String Company)
    {
      CapturadeOC capturaOC = new CapturadeOC(Company);
      List<OCList> ListaOc = await capturaOC.getListOC();
      return Json(new { data = ListaOc });
    }

    public async Task<JsonResult> SendMailRecibosAsync(String oc, String revision, String Company)
    {
      CapturadeOC captura = new CapturadeOC(Company);
      var result = await captura.SendMailRecibos(oc, revision);
      return Json(result);
    }
  }
}