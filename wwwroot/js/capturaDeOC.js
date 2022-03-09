///variable global para cargar json de lineas qr
var html;
var ocPDF;
var revisionPDF;
var ocLotePDF;
/****
 * Esta Funcion muestra los divs de captura de Orden de Compra cuando den clisk en el menu de Captura de OC
****/
function showCaptura() {
  $('.capturaOC').css('display', 'flex');
  $('.capturaOC.capturaOCDiv').css('display', 'block');
  //////esconder la otra clase/////////////////////////////
  $('.capturaOCImgs').css('display', 'none');
  $('.capturaOCImgs.capturaOCImgsDiv').css('display', 'none');
}

/****
 * Esta Funcion muestra los divs de captura de lotes de Compra cuando den clisk en el menu de Lotes
****/
function showCapturaImgs() {
  $('.capturaOCImgs').css('display', 'flex');
  $('.capturaOCImgs.capturaOCImgsDiv').css('display', 'block');
  ////////////////esconder la otra clase//////////////////////////
  $('.capturaOC').css('display', 'none');
  $('.capturaOC.capturaOCDiv').css('display', 'none');
}

/***
 * Funcion que lanza la consulta de las OC enDynamics
 * cuando el usuario teclea una y da enter
 * se crea variable global para guardar las partidas parciales
 ***/
var partidaParcial = [];
function getDataOC(oc, ev, url, raiz) {
  if (ev.which == 13) {
    var company = getCompany();
    if (company == 'atp') {
      oc = 'OC-' + oc;
    } else {
      oc = "L-OC" + oc;
    }
    $('#purchaseOrderNumber').html('');
    $('#purchaseOrderName').html('');
    $('#requestedDeliveryDate').html('');
    $('#buyerGroupId').html('');
    $('#currencyCode').html('');
    $('#defaultReceivingWarehouseId').html('');
    $('#defaultReceivingSiteId').html('');
    $('#saveOCbtn').attr('disabled', 'disabled');
    var table = $('#tablaOCs').DataTable({
      destroy: true,
      search: true,
      paging: true,
      info: false,
      ajax: {
        beforeSend: function () {
          $('body').attr('data-msj', 'Procesando Petición ...');
          $('body').addClass('load-ajax');
          $('body').append('<div class="lean-overlay"></div>');
        },
        processing: true,
        serverSide: true,
        url: url,
        type: 'POST',
        data: { OC: oc, company }
      },
      initComplete: (settings, json) => {
        if (json.dataocHead) {
          $('#purchaseOrderNumber').html(json.dataocHead.purchaseOrderNumber);
          $('#purchaseOrderName').html(json.dataocHead.purchaseOrderName);
          $('#requestedDeliveryDate').html(json.dataocHead.requestedDeliveryDate);
          $('#requestedDeliveryDate').data('fecha', json.dataocHead.requestedDeliveryDateOrig);
          $('#buyerGroupId').html(json.dataocHead.buyerGroupId);
          $('#currencyCode').html(json.dataocHead.currencyCode);
          $('#defaultReceivingWarehouseId').html(json.dataocHead.defaultReceivingWarehouseId);
          $('#defaultReceivingSiteId').html(json.dataocHead.defaultReceivingSiteId);
          $('#saveOCbtn').removeAttr('disabled');
          $('body').removeClass('load-ajax');
          $('body .lean-overlay').remove();
          if (json.data.length == 0) {
            swal('Atencion!.', 'Las lineas de esta OC han sido capturadas en su totalidad.', 'warning');
          } else if (json.data[0][1] == "SinResultados") {
            swal('Atencion!.', 'Las OC no existe!.', 'warning');
          }
          if (partidaParcial.length > 0) {
            html = '<table style="font-size: 11px;" border="1">';
            html += '    <thead>';
            html += '        <th>No. Linea</th>';
            html += '        <th>Codigo</th>';
            html += '        <th>Descripcion</th>';
            html += '        <th>Cantidad</th>';
            html += '        <th>Unidad</th>';
            html += '    </thead>';
            html += '    <tbody>';
            $(partidaParcial).each(function () {
              html += '<tr>';
              html += '    <td>' + this.numLinea + '</td>';
              html += '    <td>' + this.codigo + '</td>';
              html += '    <td>' + this.descripcion + '</td>';
              html += '    <td>' + this.canti + '</td>';
              html += '    <td>' + this.unidad + '</td>';
              html += '</tr>';
            });
            html += '    <tbody>';
            html += '</table >'
            swal({
              title: 'Atencion!. <br/> Algunas partidas estan parcialmente capturadas.',
              text: '',
              type: "warning",
              html: html
            });
          }
          partidaParcial = [];
        } else {
          $('body').removeClass('load-ajax');
          $('body .lean-overlay').remove();
          swal('Atencion!', 'Datos incorrectos', 'warning');
        }
      },
      createdRow: function (row, data, dataIndex) {
        var parcial = $(data[3]).data('parcial');
        if (parcial == 'True') {
          partidaParcial.push({ numLinea: data[0], codigo: data[1], descripcion: data[2], canti: $(data[3]).val(), unidad: data[4] });
        }
        $(row).attr('data-linedescriptionatp', data[8]);
      },
      columns: [
        { title: 'No. de Línea', width: '15%' },
        { title: 'Código', width: '15%' },
        { title: 'Descripción', width: '30%' },
        { title: 'Cantidad', width: '10%' },
        { title: 'Unidad', width: '10%' },
        { title: 'Opciones <div class="col-md-12" style="text-align:center;"><span style="font-size:10px">Todos &nbsp;</span><input type="checkbox" onclick="seleccionarTodos(this)" style="width:20px;height:20px;"/>', width: '10%' },
        { title: 'Sin Lote <div class="col-md-12" style="text-align:center;"><span style="font-size:10px">Todos &nbsp;</span><input type="checkbox" class="chkSelecSLTodos" onclick="selecTodosSinLote(this)" style="width:20px;height:20px;"/>', width: '05%' },
        { title: 'Series', width: '05%' }
      ],
      language: {
        url: raiz + "/css/spanish.json"
      }
    });
  }
}

/***
 * Funcion que lanza la consulta de las OC enDynamics
 * cuando el usuario teclea una y da enter
 * se crea variable global para guardar las partidas parciales
 ***/
function getDataOCCodigos(oc, ev, url, raiz) {
  if (ev.which == 13 || ev.type == "change") {
    var company = companyLotes(oc);
    if (company == 'atp') {
      dataOC = oc.split('-');
      oc = 'OC-' + dataOC[1];
      revision = dataOC[2];
      ocRevision = oc + "-Rev" + revision;
    } else {
      dataOC = oc.split('-');
      oc = 'L-OC' + dataOC[0];
      revision = dataOC[1];
      ocRevision = oc + "-Rev" + revision;

    }
    $('#purchaseOrderNumber').html('');
    $('#purchaseOrderName').html('');
    $('#requestedDeliveryDate').html('');
    $('#buyerGroupId').html('');
    $('#currencyCode').html('');
    $('#defaultReceivingWarehouseId').html('');
    $('#defaultReceivingSiteId').html('');
    $('#saveOCbtn').attr('disabled', 'disabled');
    var table = $('#tablaOCCodigos').DataTable({
      destroy: true,
      search: true,
      paging: true,
      info: false,
      ajax: {
        beforeSend: function () {
          $('body').attr('data-msj', 'Procesando Petición ...');
          $('body').addClass('load-ajax');
          $('body').append('<div class="lean-overlay"></div>');
        },
        processing: true,
        serverSide: true,
        url: url,
        type: 'POST',
        data: { OC: oc, ocRevision, revision, company }
      },
      initComplete: function (settings, json) {
        $('#purchaseOrderNumberImg').html(json.dataocHead.purchaseOrderNumber);
        $('#purchaseOrderNameImg').html(json.dataocHead.purchaseOrderName);
        $('#requestedDeliveryDateImg').html(json.dataocHead.requestedDeliveryDate);
        $('#requestedDeliveryDateImg').data('fecha', json.dataocHead.requestedDeliveryDateOrig);
        $('#buyerGroupIdImg').html(json.dataocHead.buyerGroupId);
        $('#currencyCodeImg').html(json.dataocHead.currencyCode);
        $('#defaultReceivingWarehouseIdImg').html(json.dataocHead.defaultReceivingWarehouseId);
        $('#defaultReceivingSiteIdImg').html(json.dataocHead.defaultReceivingSiteId);
        $('body').removeClass('load-ajax');
        $('body .lean-overlay').remove();
        if (json.data.length == 0) {
          swal('Atencion!.', 'Las lineas de esta OC han sido capturadas en su totalidad.', 'warning');
        } else if (json.data[0][1] == "SinResultados") {
          swal('Atencion!.', 'Las OC no existe!.', 'warning');
        }
        if (partidaParcial.length > 0) {
          html = '<table style="font-size: 11px;" border="1">';
          html += '    <thead>';
          html += '        <th>No. Linea</th>';
          html += '        <th>Codigo</th>';
          html += '        <th>Descripcion</th>';
          html += '        <th>Cantidad</th>';
          html += '        <th>Unidad</th>';
          html += '    </thead>';
          html += '    <tbody>';
          $(partidaParcial).each(function () {
            html += '<tr>';
            html += '    <td>' + this.numLinea + '</td>';
            html += '    <td>' + this.codigo + '</td>';
            html += '    <td>' + this.descripcion + '</td>';
            html += '    <td>' + this.canti + '</td>';
            html += '    <td>' + this.unidad + '</td>';
            html += '</tr>';
          });
          html += '    <tbody>';
          html += '</table >'
          swal({
            title: 'Atencion!. <br/> Algunas partidas estan parcialmente capturadas.',
            text: '',
            type: "warning",
            html: html
          });
        }
        partidaParcial = [];
        $('#showModImgsBtn').attr('data-ocrevision', ocRevision);
      },
      createdRow: function (row, data, dataIndex) {
        var parcial = $(data[3]).data('parcial');
        if (parcial == 'True') {
          partidaParcial.push({ numLinea: data[0], codigo: data[1], descripcion: data[2], canti: $(data[3]).val(), unidad: data[4] });
        }
        var sinLote = data[6];
        var numeroLinea = data[0];
        var item = data[1];
        if (sinLote == 'True') {
          var check = $(row).find('.chkSinLote');
          agregarSinLote(oc, numeroLinea, item, revision, check, 'createRow');
        }
      },
      columns: [
        { title: 'No. de Línea', width: '10%' },
        { title: 'Código', width: '10%' },
        { title: 'Descripción', width: '50%' },
        { title: 'Cantidad', width: '10%' },
        { title: 'Unidad', width: '10%' },
        { title: 'Sin Lote', width: '10%' }
      ],
      language: {
        url: raiz + "/css/spanish.json"
      }
    });
  }
}

/***
 * Esta funcion construye los objectos de cabecera y lineas para guaradar
 * datos en la base de datos
 ***/
function saveOrdenCompra(url) {
  var rowsSelected = $('#tablaOCs').DataTable().rows('.selected')[0].length;
  if (rowsSelected > 0) {
    var ocHeader = {
      PurchaseOrderNumber: $('#purchaseOrderNumber').html(),
      PurchaseOrderName: $('#purchaseOrderName').html(),
      RequestedDeliveryDate: $('#requestedDeliveryDate').data('fecha'),
      BuyerGroupId: $('#buyerGroupId').html(),
      CurrencyCode: $('#currencyCode').html(),
      DefaultReceivingSiteId: $('#defaultReceivingSiteId').html(),
      DefaultReceivingWarehouseId: $('#defaultReceivingWarehouseId').html()
    }

    var ocLineasArray = [];
    var lineas = $('#tablaOCs').dataTable().fnGetNodes();
    $(lineas).each(function () {
      if ($(this).hasClass('selected')) {
        var lineNumber = $(this).find('td')[0];
        var itemNumber = $(this).find('td')[1]
        var lineDescription = $(this).find('td')[2];
        var orderedPurchaseQuantity = $(this).find('td input.canti');
        var cantdyna = $(this).find('td input.canti').data('cantdyna');
        var purchaseUnitSymbol = $(this).find('td')[4];
        var sinLote = $(this).find('td input.chkSinLote').is(':checked') ? 1 : 0;
        var lineDescriptionATP = $(this).data('linedescriptionatp');
        var seriesLineasArr = getSeriesArray($(lineNumber).html());
        var seriesArr = $.map(seriesLineasArr, function (serItem) {
          return serItem.serie;
        });
        seriesStr = seriesArr.join();
        ocLineasArray.push({
          lineNumber: $(lineNumber).html(),
          purchaseOrderNumber: $('#purchaseOrderNumber').html(),
          itemNumber: $(itemNumber).html(),
          lineDescription: $(lineDescription).html(),
          orderedPurchaseQuantity: $(orderedPurchaseQuantity).val(),
          cantdyna: cantdyna,
          purchaseUnitSymbol: $(purchaseUnitSymbol).html(),
          sinLote: sinLote,
          lineDescriptionATP,
          series: seriesStr
        });
      }
    });
    $.ajax({
      url: url,
      type: 'POST',
      dataType: "JSON",
      data: { header: ocHeader, lines: ocLineasArray },
      beforeSend: function () {
        $('body').attr('data-msj', 'Guardando Datos ...');
        $('body').addClass('load-ajax');
        $('body').append('<div class="lean-overlay"></div>');
      },
      success: function (data) {
        $('body').removeClass('load-ajax');
        $('body .lean-overlay').remove();
        swal("Exito!", "Operacion realizada existosamente!", "success")
          .then(function () {
            if ($('#togBtn').is(':checked')) {
              company = 'lin';
            } else {
              company = 'atp';
            }
            sendMailRecibos(data.purchaseOrderNumber, data.revision, company);
            setTimeout(function () {
              location.reload();
            }, 500);
          });
      },
      error: function (xHrRes) {
        console.log(xHrRes);
      }
    });
  } else {
    swal("Atencion!", "Debe seleccionar al menos una linea!", "warning");
  }
}

/** Funcion para cambiar a seleccionado el 
*** renglon checkeado
**/
function checarRow(check) {
  var row = $(check).closest('tr');
  $(row).toggleClass('selected');
}

/** Funcion para cambiar a seleccionado el 
*** renglon checkeado
**/
function checarRowSerie(check) {
  $('#tablaOCs tbody tr').removeClass('selectedSerie');
  var row = $(check).closest('tr');
  var tdNumLinea = $(row).find('td')[0];
  var tdCodigo = $(row).find('td')[1];
  var tdDescripcion = $(row).find('td')[2];
  var tdCantidad = $(row).find('td')[3];
  var tdUnidad = $(row).find('td')[4];

  var lineNumber = $(tdNumLinea).html();
  var codigo = $(tdCodigo).html();
  var descripcion = $(tdDescripcion).html();
  var cantidad = $(tdCantidad).find('input.canti').val();
  var unidad = $(tdUnidad).html();
  var oc = $('#purchaseOrderNumber').html();

  if ($(check).is(':checked')) {
    $(row).toggleClass('selectedSerie');
    var htmlSerie = getHtmlSeries(codigo, descripcion, cantidad, unidad, lineNumber, oc);
    $('#numerosSerie').html(htmlSerie);
  } else {
    $('.nav-link.nav-series').click();
    $('#numerosSerie').empty();
    deleteSeries(lineNumber);
  }
}

/** Funcion para seleccionar todos los
*** renglones de la datatable
**/
function seleccionarTodos(obj) {
  var lineas = $('#tablaOCs').dataTable().fnGetNodes();
  var row = $(obj).closest('tr');
  $(lineas).find('.chkSeleccionar').prop('checked', false);
  $(lineas).removeClass('selected');
  if ($(obj).is(':checked')) {
    $(lineas).find('.chkSeleccionar').prop('checked', 'checked');
    $(lineas).toggleClass('selected');
  } else {
    $(lineas).find('.chkSinLote').prop('checked', false);
    $(row).find('.chkSelecSLTodos').prop('checked', false);
  }
}

/** Funcion para seleccionar todos los
*** renglones de la datatable sin lote
**/
function selecTodosSinLote(obj) {
  var lineas = $('#tablaOCs').dataTable().fnGetNodes();
  $(lineas).find('.chkSinLote').prop('checked', false);
  if ($(obj).is(':checked')) {
    $(lineas).find('.chkSinLote').prop('checked', 'checked');
  }
}

/*** Funcion para cargar el swal de los
 *** lotes y comentarios
***/
function cargarLotesComent(purchaseOrderNumber, lineNumber, itemNumber, description, row, cant) {
  ////////////////////html para el swal/////////////////////////////
  html = '';
  html += '<hr/>';
  html += '<div class="row" style="max-height:155px;overflow-y:scroll">';
  html += '	<div class="col-md-2">';
  html += '		<label>Linea:</label>';
  html += '		<span style="font-size:55px;">' + lineNumber + '</span>';
  html += '	</div>';
  html += '	<div class="col-md-8" id="loteComent">';
  html += '       <div class="row">';
  html += '		    <div class="col-md-2 cant">';
  html += '               <label class="form-label">Cantidad:</label>';
  html += '			    <input type="text" class="cantGen" data-index="0" id="cantGen0" style="width:100%;margin-top:15%;color:#545454" value="' + cant + '" />';
  html += '		    </div>';
  html += '		    <div class="col-md-4 lote">';
  html += '               <label class="form-label">Lote:</label>';
  html += '			    <input type="text" class="loteGen" data-index="0" id="loteGen0" data-lote="LSD-0" style="width:100%;margin-top:5%"/>';
  html += '		    </div>';
  html += '		    <div class="col-md-3 coment">';
  html += '               <label class="form-label">Comentario:</label>';
  html += '			    <input type="text" class="comentGen" data-index="0" id="comentGen0" data-comentario="CSD-0" style="width:100%;margin-top:5%"/>';
  html += '		    </div>';
  html += '		    <div class="col-md-3 serie">';
  html += '               <label class="form-label">Serie:</label>';
  html += '			    <input type="text" class="serieGen" data-index="0" id="serieGen0" data-serie="SSD-0" style="width:100%;margin-top:5%"/>';
  html += '		    </div>';
  html += '        </div>';
  html += '	</div>';
  html += '	<div class="col-md-2">';
  html += '		<i class="fa fa-plus" aria-hidden="true" style="cursor:pointer" onclick="addNewLineloteComent(' + cant + ');"></i>';
  html += '	</div>';
  html += '</div>';
  html += '<hr/>';
  html += '<div class="row">';
  html += '    <div class="col-md-12">';
  html += '        <span id="lineDescription" style="font-size:15px;font-weight:bolder">' + description + '</span>';
  html += '    </div>';
  html += '</div>';
  html += '<input type="hidden" value="' + purchaseOrderNumber + '" id="purchaseOrderNumberHidden">';
  html += '<input type="hidden" value="' + lineNumber + '" id="lineNumberHidden">';
  html += '<input type="hidden" value="' + itemNumber + '" id="itemNumberHidden">';
  titleHtml = '<div class="row">';
  titleHtml += '  <div class="col-md-3" style="text-align:left"><i class="fa fa-arrow-circle-o-left fa-2x" aria-hidden="true" style="color:#2E86C1;cursor:pointer" onclick="swal.closeModal();"></i></div>';
  titleHtml += '  <div class="col-md-8" style="padding:10px">Capture los lotes, series y comentarios</div>';
  titleHtml += '  <div class="col-md-1">&nbsp;</div>';
  titleHtml += '</div>';
  swal({
    type: 'info',
    html: html,
    title: titleHtml,
    allowOutsideClick: false,
    allowEnterKey: false,
    width: 1000,
    preConfirm: function (result) {
      var purchaseOrderNumber = $('#purchaseOrderNumberHidden').val();
      var lineNumber = $('#lineNumberHidden').val();
      var itemNumber = $('#itemNumberHidden').val();
      dataOC = purchaseOrderNumber.split('-');
      if (company == 'atp') {        
        oc = 'OC-' + dataOC[1];
        revision = dataOC[2];
        ocRevision = oc + "-Rev" + revision;
        ocLote = 'OC' + dataOC[1];
      } else {
        oc = 'L-OC' + dataOC[0];
        revision = dataOC[1];
        ocRevision = oc + "-Rev" + revision;
        ocLote = 'LOC' + dataOC[0];
      }
      var data = { purchaseOrderNumber: oc, lineNumber, itemNumber };
      ///////////validar lotes vacios////////////////////////////////
      var validoLote = true;
      var lotArr = $('.loteGen');
      $(lotArr).each(function () {
        if (this.value == '') {
          validoLote = false;
          $(this).css('border', 'solid 2px red');
        }
      });
      /////////////////////////////////////////////////////////////
      ///////////validar cantidades////////////////////////////////
      var validoCant = true;
      var cantArr = $('.cantGen');
      var estilo = '';
      suma = 0;
      $(cantArr).each(function () {
        suma += eval(this.value);
      });
      if (suma != cant) {
        estilo = '<span style="color:orange;text-decoration: underline;">' + suma + '</span>';
        if (suma > cant) {
          estilo = '<span style="color:red;text-decoration: underline;">' + suma + '</span>';
        }
        validoCant = false;
      }
      /////////////////////////////////////////////////////////////
      return new Promise(function (resolve, reject) {
        if (!validoLote) {
          reject('El campo no puede ir vacio.');
        } else if (!validoCant) {
          reject('La cantidad a recibir es: <span style="text-decoration: underline;">' + cant + '</span><br>Y la cantidad en lotes es: ' + estilo);
        }
        resolve(data);
      });
    },
    onOpen: function () {
      dataOC = purchaseOrderNumber.split('-');
      if (company == 'atp') {
        oc = 'OC-' + dataOC[1];
        revision = dataOC[2];
        ocRevision = oc + "-Rev" + revision;
        ocLote = 'OC' + dataOC[1];
      } else {
        oc = 'L-OC' + dataOC[0];
        revision = dataOC[1];
        ocRevision = oc + "-Rev" + revision;
        ocLote = 'LOC' + dataOC[0];
      }
      getImagesRoutes(ocRevision, lineNumber, itemNumber, oc, revision);
    }
  }).then(function (result) {
    var Detalle = getDataLoteComent();
    //*** Ajax para guardar los datos de los comentarios
    url = $('#urlSaveImagesComent').data('request-url');
    DataOrden = result;
    $.ajax({
      url: url,
      type: 'POST',
      dataType: "JSON",
      data: { DataLote: Detalle.DataLote, ComentImagen: Detalle.ComentImagen, DataOrden, revision },
      beforeSend: function () {
        $('body').attr('data-msj', 'Guardando Datos ...');
        $('body').addClass('load-ajax');
        $('body').append('<div class="lean-overlay"></div>');
      },
      success: function (data) {
        $('body').removeClass('load-ajax');
        $('body .lean-overlay').remove();
        if (data.status == 'Exito') {
          swal("Exito!", "Operacion realizada existosamente!", "success")
            .then(function () {
              $(row).css('background-color', 'green');
              $(row).css('color', 'white');
              $(row).addClass('completed');
              var rowsCompleteds = $('#tablaOCCodigos').DataTable().rows('.completed');
              var rowsQuant = $('#tablaOCCodigos').DataTable().rows();
              if (rowsCompleteds[0].length == rowsQuant[0].length) {
                swal({
                  type: 'question',
                  html: "",
                  title: 'La totalidad de las lineas han sido capturadas. Desea terminar la captura?',
                  allowOutsideClick: false,
                  allowEnterKey: false,
                  showCancelButton: true,
                  cancelButtonText: 'Continuar captura de lotes',
                  confirmButtonText: 'Terminar captura y enviar correo?'
                }).then(function (result) {
                  if (result) {
                    dataOC = purchaseOrderNumber.split('-');
                    if (company == 'atp') {
                      oc = 'OC-' + dataOC[1];
                      revision = dataOC[2];
                      ocRevision = oc + "-Rev" + revision;
                      ocLote = 'OC' + dataOC[1];
                    } else {
                      oc = 'L-OC' + dataOC[0];
                      revision = dataOC[1];
                      ocRevision = oc + "-Rev" + revision;
                      ocLote = 'LOC' + dataOC[0];
                    }
                    convertToPDF(oc, revision, ocLote,company);
                    setTimeout(function () {
                      location.reload();
                    }, 500);
                  }
                }, function (dismiss) {
                  if (dismiss == 'cancel') {
                    console.log('Seguiran capturando Lotes.');
                  }
                });

              }
            });
        }
      },
      error: function (xHrRes) {
        console.log(xHrRes);
      }
    });
  });
}

/*** funcion para agregar mas lotes y comentarios
**** en los inputs del swal
***/
function addNewLineloteComent(cant) {
  var indexArr = $('.trashLotComent').map(function () {
    return $(this).data('index')
  });
  var index = 1;
  if (indexArr.length > 0) {
    index = Math.max(...indexArr) + 1;
  }
  $('#loteComent .row .cant').append('<input type="text" id="cantGen' + index + '" class="cantGen" data-index="' + index + '"  style="width:100%;margin-top:15%;color:#545454" value="' + cant + '" />');
  $('#loteComent .row .lote').append('<input type="text" id="loteGen' + index + '" class="loteGen" data-index="' + index + '" data-lote="LSD-' + index + '" style="width:100%;margin-top:5%;">');
  $('#loteComent .row .coment').append('<input type="text" id="comentGen' + index + '" class="comentGen" data-index="' + index + '" data-comentario="CSD-' + index + '" style="width:80%;margin-top:5%;margin-right:5%">');
  $('#loteComent .row .serie').append('<input type="text" id="serieGen' + index + '" class="serieGen" data-index="' + index + '" data-serie="SSD-' + index + '" style="width:80%;margin-top:5%;margin-right:5%"><i class="fa fa-trash trashLotComent" aria-hidden="true" id="trash' + index + '" onclick="removeLotComent(this)" style="cursor:pointer" data-index="' + index + '"></i>');
}

/*** Funcion para remover las lineas de los comentarios
**** o lotes
***/
function removeLotComent(obj) {
  var index = $(obj).data('index');
  $('#loteGen' + index).remove();
  $('#comentGen' + index).remove();
  $('#cantGen' + index).remove();
  $('#serieGen' + index).remove();
  $('#trash' + index).remove();
  $('#tableLotProv tbody tr')[index].remove()
  ///////borrar el comentario y el lote del servidor/////////////////
  var url = $('#urlRemoveLoteComent').data('request-url');
  var data = $(obj).data();
  $.ajax({
    url: url,
    type: 'POST',
    dataType: "JSON",
    data: { data },
    beforeSend: function () {
      $('body').attr('data-msj', 'Eliminando registros!. ...');
      $('body').addClass('load-ajax');
      $('body').append('<div class="lean-overlay"></div>');
    },
    success: function (res) {
      if (res.data.msg != 'Fallo') {
        $('body').removeClass('load-ajax');
        $('body .lean-overlay').remove();
      } else {
        console.log('Fallo');
      }
    },
    error: function (xHRres) {
      console.log('error' + xHRres);
    }
  });
}

/*** Funcion para devolver la ruta 
**** de las imagenes de las lineas
**** de la oc
***/
function getImagesRoutes(purchaseOrderNumber, lineNumber, itemNumber, oc, revision) {
  var url = $('#urlImagesPaths').data('request-url');
  $.ajax({
    url: url,
    type: 'POST',
    dataType: "JSON",
    data: { purchaseOrderNumber, lineNumber, itemNumber, oc, revision },
    beforeSend: function () {
      $('body').attr('data-msj', 'Obteniendo rutas de imagenes ...');
      $('body').addClass('load-ajax');
      $('body').append('<div class="lean-overlay"></div>');
    },
    success: function (data) {
      $('body').removeClass('load-ajax');
      $('body .lean-overlay').remove();
      htmlBody = '       <div class="row">';
      htmlBody += '		    <div class="col-md-2 cant">';
      htmlBody += '               <label class="form-label">Cantidad:</label>';
      $(data.dataLoteComent).each(function (index) {
        if (index == 0) {
          htmlBody += '		    <input type="text" class="cantGen" data-index="' + index + '" id="cantGen' + index + '" style="width:100%;margin-top:15%;color:#545454" value="' + this.cantidad + '"/>';
        } else {
          htmlBody += '           <input type="text" id="cantGen' + index + '" class="cantGen" data-index="' + index + '" style="width:100%;margin-top:15%;color:#545454" value="' + this.cantidad + '"/>';
        }
      })
      htmlBody += '        </div>';
      htmlBody += '		    <div class="col-md-4 lote">';
      htmlBody += '               <label class="form-label">Lote:</label>';
      if (data.dataLoteComent.length > 0) {
        htmlTablaLote = '<div class="row" style="position: absolute;right: 710px;transform: translate(100%, 0);background-color:white">';
        htmlTablaLote += '	<div class="col-md-12">';
        htmlTablaLote += '		<table class="table" id="tableLotProv">';
        htmlTablaLote += '			<thead>';
        htmlTablaLote += '				<tr>';
        htmlTablaLote += '					<th>LoteProveedor</th>';
        htmlTablaLote += '					<th>Comentario Cedis</th>';
        htmlTablaLote += '				</tr>';
        htmlTablaLote += '			</thead>';
        htmlTablaLote += '			<tbody>';
        $(data.dataLoteComent).each(function (index) {
          htmlTablaLote += '				<tr>';
          htmlTablaLote += '					<td style="text-align:center">' + this.lote + '</td>';
          htmlTablaLote += '					<td style="text-align:center">' + this.comentario + '</td>';
          htmlTablaLote += '				</tr>';
          if (index == 0) {
            htmlBody += '			    <input type="text" class="loteGen" data-index="' + index + '" id="loteGen' + index + '" data-lote="' + this.lote + '" style="width:100%;margin-top:5%" value="' + this.loteATP + '"/>';
          } else {
            htmlBody += '                <input type="text" id="loteGen' + index + '" class="loteGen" data-index="' + index + '" data-lote="' + this.lote + '" style="width:100%;margin-top:5%;" value="' + this.loteATP + '">';
          }
        });
        htmlTablaLote += '			</tbody>';
        htmlTablaLote += '		</table>';
        htmlTablaLote += '	</div>';
        htmlTablaLote += '</div>';
        htmlBody += '		    </div>';
        htmlBody += '		    <div class="col-md-3 coment">';
        htmlBody += '               <label class="form-label">Comentario:</label>';
        $(data.dataLoteComent).each(function (index) {
          if (index == 0) {
            htmlBody += '		    <input type="text" class="comentGen" data-index="' + index + '" id="comentGen' + index + '" data-comentario="' + this.comentario + '" style="width:100%;margin-top:5%" value="' + this.comentarioATP + '"/>';
          } else {
            htmlBody += '           <input type="text" id="comentGen' + index + '" class="comentGen" data-index="' + index + '" data-comentario="' + this.comentario + '" style="width:80%;margin-top:5%;margin-right:5%" value="' + this.comentarioATP + '"/>';
          }
        });
        htmlBody += '        </div>';
        htmlBody += '		    <div class="col-md-3 coment">';
        htmlBody += '               <label class="form-label">Serie:</label>';
        $(data.dataLoteComent).each(function (index) {
          if (index == 0) {
            htmlBody += '		    <input type="text" class="serieGen" data-index="' + index + '" id="serieGen' + index + '" data-serie="' + this.serieATP + '" style="width:100%;margin-top:5%" value="' + this.serieATP + '"/>';
          } else {
            htmlBody += '           <input type="text" id="serieGen' + index + '" class="serieGen" data-index="' + index + '" data-serie="' + this.serieATP + '" style="width:80%;margin-top:5%;margin-right:5%" value="' + this.serieATP + '"/><i class="fa fa-trash trashLotComent" aria-hidden="true" id="trash' + index + '" onclick="removeLotComent(this)" style="cursor:pointer" data-index="' + index + '" data-idcoment="' + this.idComent + '" data-purchaseordernumber="' + this.purchaseOrderNumber + '" data-linenumber="' + this.lineNumber + '", data-itemnumber="' + this.itemNumber + '" data-serie="' + this.serieATP + '"></i>';
          }
        })
        htmlBody += '        </div>';
        htmlBody += '		    </div>';
        $('#loteComent').html(htmlBody);
        //$('.swal2-container.swal2-shown').append(htmlTablaLote);
      }
    },
    error: function (xHrRes) {
      console.log(xHrRes);
    }
  });
}

/** funcion para obtener los datos
*** de los comentarios y las lineas de los lotes
*** para su procesamiento en la db
**/
function getDataLoteComent() {
  var lotArr = $('.loteGen');
  var comentImg = $('.comentImgGen');
  var data = { DataLote: [], ComentImagen: [] };
  $(lotArr).each(function () {
    index = $(this).data('index');
    lote = $(this).data('lote');
    loteATP = this.value;
    comentario = $('#comentGen' + index).data('comentario');
    comentarioATP = $('#comentGen' + index).val();
    serieATP = $('#serieGen' + index).val();
    cantidad = $('#cantGen' + index).val();
    data.DataLote.push({ lote, comentario, loteATP, comentarioATP, cantidad, serieATP });
  });
  $(comentImg).each(function () {
    var imgNum = $(this).attr('id');
    var nombre = $(this).data('nombre');
    data.ComentImagen.push({ comentario: this.value, imagen: imgNum, nombre });
  });
  return data;
}

/*** funcion para cargar la tabla del visor
 *** de ordenes de compra para generar QR y enviar por correo
***/
function cargarTablaOC() {
  var url = $('#urlCargaTablaInicial').data('request-url');
  var raiz = $('#urlCargaTablaInicial').data('raiz');
  var table = $('#tablaVisorQR').DataTable({
    destroy: true,
    search: true,
    paging: true,
    info: false,
    ajax: {
      beforeSend: function () {
        $('body').attr('data-msj', 'Procesando Petición ...');
        $('body').addClass('load-ajax');
        $('body').append('<div class="lean-overlay"></div>');
      },
      processing: true,
      serverSide: true,
      url: url,
      type: 'POST',
      data: {}
    },
    initComplete: (settings, json) => {
      $('body').removeClass('load-ajax');
      $('body .lean-overlay').remove();
    },
    createdRow: function (row, data, index) {
      $(row).attr('data-ocrevision', data[0]);
      $(row).attr('data-revision', data[8]);
      $(row).attr('data-oc', data[9]);
      $(row).css('cursor', 'pointer');
    },
    columns: [
      { title: 'Orden de Compra', width: '15%' },
      { title: 'Proveedor', width: '40%' },
      { title: 'Fecha de entrega', width: '15%' },
      { title: 'Grupo de compras', width: '10%' },
      { title: 'Moneda', width: '10%' },
      { title: 'Sitio', width: '10%' },
      { title: 'Almacen', width: '10%' },
      { title: 'Status', width: '10%' }
    ],
    language: {
      url: raiz + "/css/spanish.json"
    }
  });
}

/*** funcion para cargar las lineas de la orden
 *** de compra seleccionada en visor qr
***/
function cargarTablaLineas(oc, revision) {
  var url = $('#urlCargaTablaLineas').data('request-url');
  var raiz = $('#urlCargaTablaInicial').data('raiz');
  var table = $('#tablaVisorLineasQR').DataTable({
    destroy: true,
    search: true,
    paging: true,
    info: false,
    ajax: {
      beforeSend: function () {
        $('body').attr('data-msj', 'Procesando Petición ...');
        $('body').addClass('load-ajax');
        $('body').append('<div class="lean-overlay"></div>');
      },
      processing: true,
      serverSide: true,
      url: url,
      type: 'POST',
      data: { oc, revision }
    },
    initComplete: function (settings, json) {
      $('body').removeClass('load-ajax');
      $('body .lean-overlay').remove();
      html = json;
      ocPDF = html.data[0][9];
      revisionPDF = html.data[0][10];
      ocLotePDF = html.data[0][11];
      $("#buttonPDF").html(html.data[0][12]);
    },
    columns: [
      { title: 'No. de Línea', width: '10%' },
      { title: 'Código', width: '10%' },
      { title: 'Descripción', width: '20%' },
      { title: 'Cantidad', width: '10%' },
      { title: 'Unidad', width: '10%' },
      { title: 'Lote', width: '10%' },
      { title: 'QR', width: '10%' },
      { title: 'Comentario', width: '10%' },
      { title: 'Serie', width: '10%' }
    ],
    language: {
      url: raiz + "/css/spanish.json"
    }
  });
}

/*** funcion para convrtir a pdf las 
 *** lineas de la  OC
***/
function convertToPDF(ocPDF, revisionPDF, ocLotePDF) {
  var url = $('#urlEnviarPDF').data('request-url');
  if (ocPDF  && revisionPDF  && ocLotePDF ) {
    var content = JSON.stringify(html)
    $('#html').val(content);
    $('#pdfForm').submit();
    company = companyLotes(ocPDF + '-' + revisionPDF);
    //////////////////////enviar correo de pdf////////////////////////////
    $.ajax({
      url: url,
      type: 'POST',
      dataType: 'json',
      data: { ocPDF, revisionPDF, ocLotePDF,company },
      beforeSend: function () {
        $('body').attr('data-msj', 'Enviando correo!. ...');
        $('body').addClass('load-ajax');
        $('body').append('<div class="lean-overlay"></div>');
      },
      success: function (res) {
        $('body').removeClass('load-ajax');
        $('body .lean-overlay').remove();
      },
      error: function (xHRres, res, res2) {
        console.log(xHRres);
        console.log(res);
        console.log(res2);
      }
    });
  } else {
    swal("Error!", "Elija una orden de Compra", "warning")
  }
}

/*** Funcion para mostrar el carrusel de las imagenes
**** en una pantalla
**** aparte
***/
function showCarrusel(obj) {
  oc = obj.dataset['ocrevision'];
  console.log(obj);
  console.log('oc', oc);
  var host = window.location.host;
  //Pruebas Local
  //window.open("https://" + host + "/CapturadeOC/Carrusel?purchaseOrderNumber=" + oc);
  //Produtivo Test
  window.open("https://" + host + "/CodigosQRTest/CapturadeOC/Carrusel?purchaseOrderNumber=" + oc);
  //Produtivo 
  //window.open("https://" + host + "/CodigosQR/CapturadeOC/Carrusel?purchaseOrderNumber=" + oc);
}

/*** Funcion para mandar correo de captura de
**** orden de compra dirigida a recibos (CEDIS)
***/
function sendMailRecibos(oc, revision,company) {
  var url = $('#urlSendMailRecibos').data('request-url');
  $.ajax({
    url: url,
    type: 'POST',
    dataType: "JSON",
    data: { oc, revision,company },
    beforeSend: function () {
      $('body').attr('data-msj', 'Enviando correo ...');
      $('body').addClass('load-ajax');
      $('body').append('<div class="lean-overlay"></div>');
    },
    success: function (data) {
      $('body').removeClass('load-ajax');
      $('body .lean-overlay').remove();
    },
    error: function (xHrRes) {
      console.log(xHrRes);
    }
  });
}

/**** Funcion para agregar lineas sin lote
***** se guardan con lote vacio para los articulos
***** sin autobatch
****/
function agregarSinLote(oc, lineNumber, itemNumber, revision, obj, tipo) {
  var row = $(obj).closest('tr');
  var orderedPurchaseQuantity = $(row).find('td input.canti');
  var cantidad = $(orderedPurchaseQuantity).val();
  var Detalle = { DataLote: [{ lote: 'LSD-0', comentario: 'CSD-0', loteATP: '', comentarioATP: '', cantidad }], ComentImagen: [] };
  //*** Ajax para guardar los datos de los comentarios
  url = $('#urlSaveImagesComent').data('request-url');
  DataOrden = { purchaseOrderNumber: oc, lineNumber, itemNumber };
  $.ajax({
    url: url,
    type: 'POST',
    dataType: "JSON",
    data: { DataLote: Detalle.DataLote, ComentImagen: Detalle.ComentImagen, DataOrden, revision },
    beforeSend: function () {
      $('body').attr('data-msj', 'Guardando Datos ...');
      $('body').addClass('load-ajax');
      $('body').append('<div class="lean-overlay"></div>');
    },
    success: function (data) {
      $('body').removeClass('load-ajax');
      $('body .lean-overlay').remove();
      if (data.status == 'Exito') {
        $(obj).prop('disabled', 'disabled');
        if (tipo == 'check') {
          swal("Exito!", "Operacion realizada existosamente!", "success")
            .then(function () {
              $(row).css('background-color', 'green');
              $(row).css('color', 'white');
              $(row).addClass('completed');
              var rowsCompleteds = $('#tablaOCCodigos').DataTable().rows('.completed');
              var rowsQuant = $('#tablaOCCodigos').DataTable().rows();
              if (rowsCompleteds[0].length == rowsQuant[0].length) {
                swal({
                  type: 'question',
                  html: "",
                  title: 'La totalidad de las lineas han sido capturadas. Desea terminar la captura?',
                  allowOutsideClick: false,
                  allowEnterKey: false,
                  showCancelButton: true,
                  cancelButtonText: 'Continuar captura de lotes',
                  confirmButtonText: 'Terminar captura y enviar correo?'
                }).then(function (result) {
                  if (result) {
                    ocLote = oc.replace('-', '');
                    convertToPDF(oc, revision, ocLote);
                    setTimeout(function () {
                      location.reload();
                    }, 5000);
                  }
                }, function (dismiss) {
                  if (dismiss == 'cancel') {
                    console.log('Seguiran capturando Lotes.');
                  }
                });
              }
            });
        } else {
          $(obj).prop('checked', 'checked');
          $(row).css('background-color', 'green');
          $(row).css('color', 'white');
          $(row).addClass('completed');
          var rowsCompleteds = $('#tablaOCCodigos').DataTable().rows('.completed');
          var rowsQuant = $('#tablaOCCodigos').DataTable().rows();
          if (rowsCompleteds[0].length == rowsQuant[0].length) {
            swal({
              type: 'question',
              html: "",
              title: 'La totalidad de las lineas han sido capturadas. Desea terminar la captura?',
              allowOutsideClick: false,
              allowEnterKey: false,
              showCancelButton: true,
              cancelButtonText: 'Continuar captura de lotes',
              confirmButtonText: 'Terminar captura y enviar correo?'
            }).then(function (result) {
              if (result) {
                ocLote = oc.replace('-', '');
                convertToPDF(oc, revision, ocLote);
                setTimeout(function () {
                  location.reload();
                }, 5000);
              }
            }, function (dismiss) {
              if (dismiss == 'cancel') {
                console.log('Seguiran capturando Lotes.');
              }
            });
          }
        }
      }
    },
    error: function (xHrRes) {
      console.log(xHrRes);
    }
  });
}

/***** Funcion para mostrar el modal de los numeros de serie
****** Se manda el numero de articulo, cantidad, unidad y descripcion
*****/
function showSeriesModal(codigoArticulo, descripcion, unidad, cantidad) {
  //var url = $('#urlAsignarSeries').data('request-url');
  //$.ajax({
  //    url: url,
  //    type: 'POST',
  //    dataType: "JSON",
  //    data: { codigoArticulo , descripcion, unidad, cantidad },
  //    beforeSend: function () {
  //        $('body').attr('data-msj', 'Guardando Datos ...');
  //        $('body').addClass('load-ajax');
  //        $('body').append('<div class="lean-overlay"></div>');
  //    },
  //    success: function (data) {
  //        $('body').removeClass('load-ajax');
  //        $('body .lean-overlay').remove();
  //        $('#modalSeries').html(data);
  //        $('#modalSeries').show();
  //        console.log('data: ',data);
  //    }
  //});
}

/****** Funcion para hacer fade el tab de series
******/
$('.nav-link.nav-series').on('click', function () {
  /////////////////ocultar el div de las series////////////////////////////
  if ($(this).hasClass('active')) {
    var tab = $(this).attr('href');
    var series = $(tab).parent().parent('div');
    if ($(series).hasClass('oculta')) {
      $(series).removeClass('oculta');
      $(series).addClass('visible');
    } else {
      $(series).removeClass('visible');
      $(series).addClass('oculta');
    }
  }
  ////////////////////////////////////////////////////////////////////////////
});

/***** Funcion para devolver el html de los inputs para
****** los numeros de serie
*****/
function getHtmlSeries(codigo, descripcion, cantidad, unidad, lineNumber, oc) {
  var serieXLinea = getSeriesArray(lineNumber);
  var j = 0;
  html = '<div class="row">';
  html += '<div class="col-md-12" style="background-color:#007bff;color:white;border:solid 1px blue;line-height:15px;padding-bottom:10px;">';
  html += '<span style="font-size:10px;"><b>' + codigo + '</b> ' + descripcion + '</span><br/><span style="font-size:10px;"><b>Cantidad:</b> ' + cantidad + ' ' + unidad + '</span>';
  html += '</div>';
  html += '</div>';
  html += '<div class="row" style="background-color:navajowhite">';
  html += '<div class="col-md-4">';
  for (i = 1; i <= cantidad; i++) {
    html += '<label for="serie' + i + '"style="font-size:11px;">Serie #' + i + ' </label>';
    if (serieXLinea.length > 0) {
      html += '<input type="text" class="form-control serieInput" id="serie' + i + '" data-codigo="' + codigo + '" data-lineNumber="' + lineNumber + '" data-oc="' + oc + '" style="padding-top:0px;padding-bottom:0px;line-height:1px;font-size:10px;width:100%;" value="' + serieXLinea[j].serie + '">';
    } else {
      html += '<input type="text" class="form-control serieInput" id="serie' + i + '" data-codigo="' + codigo + '" data-lineNumber="' + lineNumber + '" data-oc="' + oc + '" style="padding-top:0px;padding-bottom:0px;line-height:1px;font-size:10px;width:100%;">';
    }
    j++;
  }
  html += '<br/></div>';////primera division
  html += '<div class="col-md-8">';
  html += '<button type="button" class="btn btn-default" aria-label="Left Align" onclick="setSeriesArray(' + lineNumber + ')">';
  html += '<i class="fa fa-check-square-o fa-3x" aria-hidden="true" ></i> Asignar series';
  html += '</button >';
  html += '<button type="button" class="btn btn-default" aria-label="Left Align" onclick="deleteSeries(' + lineNumber + ')">';
  html += '<i class="fa fa-refresh fa-3x" aria-hidden="true" ></i> Limpiar campos';
  html += '</button >';
  html += '</div>';
  html += '</div>';
  return html;
}

/***** Funcion para asignar los numeros de serie
****** se declara una variable global  series para guardadr los objectos
*****/
var series = [];
function setSeriesArray(lineNumber) {
  deleteSeries(lineNumber, 1);
  $('.serieInput').each(function () {
    var oc = $(this).data('oc');
    var lineNumber = $(this).data('linenumber');
    var codigo = $(this).data('codigo');
    var serie = $(this).val();
    var serieObj = { oc, lineNumber, codigo, serie };
    series.push(serieObj);
  });
}

/***** Funcion para traer las series guardadas en las lineas
*****/
function getSeriesArray(lineNumber) {
  var seriesXLinea = $.map(series, function (serieItem) {
    if (serieItem.lineNumber == lineNumber) {
      return serieItem;
    }
  });
  return seriesXLinea;
}

/***** funcion para eliminar las series de un articulo
****** se alimenta el numero de linea para hacer el map
*****/
function deleteSeries(lineNumber, set = 0) {
  series = $.map(series, function (serieItem) {
    if (serieItem.lineNumber != lineNumber) {
      return serieItem;
    }
  });
  if (set == 0) {
    $('.serieInput').val('');
  }
}

function getCompany() {
  var inputs = document.getElementById("togBtn");
  var company = 'atp'
  if (inputs.checked == false) {
    //alert('ATP')
    company = 'atp'
  }
  if (inputs.checked == true) {
    //alert('LIN')
    company = 'lin'
  }
  return company
}

function changePrefix() {
  var inputs = document.getElementById("togBtn");
  if (inputs.checked == false) {
    //alert('ATP')
    document.getElementById("prefix").textContent = "OC-";
  }
  if (inputs.checked == true) {
    //alert('LIN')
    document.getElementById("prefix").textContent = "L-OC";
  }
}


function companyLotes(str) {
  var regAtp = '^OC-[0-9]{9}-[0-9]$'
  var regLin = '^[0-9]{6}-[0-9]$'
  var company = 'atp';
  if (str.match(regLin)) {
    //alert('lin')
    company = 'lin'
  } else if (str.match(regAtp)) {
    //alert('atp')
    company = 'atp'
  } else {
    company = null;
  }
  return company
}