/***
 * Funcion que lanza la consulta de las OC enDynamics
 * cuando el usuario teclea una y da enter
 * se crea variable global para guardar las partidas parciales
 ***/
var partidaParcial = [];
function getDataOCCedis(oc, ev, url, raiz) {
  if (ev.which == 13 || ev.type == "change" && oc.length >= 12 || oc.length<=15) {
    dataOC = oc.split('-');
    console.log(dataOC)
    var company = companyCedis(oc);
    if (company == 'atp') {
      oc = ''
      oc = 'OC-' + dataOC[0];
      revision = dataOC[1];
      ocRevision = oc + "-Rev" + revision;
    } else if (company == 'lin') {
      oc = ''
      oc = 'L-' + dataOC[1];
      revision = dataOC[2];
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
        data: { OC: oc, revision, ocRevision, company }
      },
      initComplete: function (settings, json) {
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
          html += '        <th>SubRevision</th>';
          html += '    </thead>';
          html += '    <tbody>';
          $(partidaParcial).each(function () {
            html += '<tr>';
            html += '    <td>' + this.numLinea + '</td>';
            html += '    <td>' + this.codigo + '</td>';
            html += '    <td>' + this.descripcion + '</td>';
            html += '    <td>' + this.canti + '</td>';
            html += '    <td>' + this.unidad + '</td>';
            html += '    <td>' + this.imagenDrop + '</td>';
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
        $('#showModImgsBtn').attr('data-ocoriginal', oc);
        $('#showModImgsBtn').attr('data-revision', revision);
      },
      createdRow: function (row, data, dataIndex) {
        var parcial = $(data[3]).data('parcial');
        if (parcial == 'True') {
          partidaParcial.push({ numLinea: data[0], codigo: data[1], descripcion: data[2], canti: $(data[3]).val(), unidad: data[4], imagenDrop: data[5] });
        }
      },
      columns: [
        { title: 'No. de Línea', width: '15%' },
        { title: 'Código', width: '15%' },
        { title: 'Descripción', width: '45%' },
        { title: 'Cantidad', width: '15%' },
        { title: 'Unidad', width: '10%' },
        { title: 'SubRevision', width: '10%' }
      ],
      language: {
        url: raiz + "/css/spanish.json"
      }
    });
  }
}

  /***
   * Funcion para subir imagenes por lineas
   * los datos son traidos del dropzone
   ***/
  function showModalImgs(obj) {
    var purchaseOrderNumber = $(obj).data('ocoriginal');
    var purchaseOrderNumberRev = $(obj).data('ocrevision');
    var revision = $(obj).data('revision');
    var company = companyCedis(purchaseOrderNumber + "-"+revision);
    body = '';
    body += '<form action="Cedis/uploadFiles" method="POST" enctype="multipart/form-data" class="dropzone">';
    //body += '    <div class="dz-preview dz-file-preview">';
    //body += '        <div class="dz-details">';
    //body += '            <div class="dz-filename"><span data-dz-name></span></div>';
    //body += '            <div class="dz-size" data-dz-size></div>';
    //body += '            <img data-dz-thumbnail />';
    //body += '        </div>';
    //body += '        <div class="dz-progress"><span class="dz-upload" data-dz-uploadprogress></span></div>';
    //body += '        <div class="dz-success-mark"><span>✔</span></div>';
    //body += '        <div class="dz-error-mark"><span>✘</span></div>';
    //body += '        <div class="dz-error-message"><span data-dz-errormessage></span></div>';
    //body += '    </div>';
    body += '    <div id="" style="display:none"><input type="hidden" name="purchaseOrderNumber" value="' + purchaseOrderNumber + '"/></div>';
    body += '    <div id="" style="display:none"><input type="hidden" name="revision" value="' + revision + '"/></div>';
    body += '    <div id="" style="display:none"><input type="hidden" name="purchaseOrderNumberRev" value="' + purchaseOrderNumberRev + '"/></div>';
    body += '    <div id="" style="display:none"><input type="hidden" name="Company" value="' + company + '"/></div>';
    body += '</form>';
    body += '<div class="msg-error"></div>';

    swal({
      title: "Subir Imagenes",
      text: "Subir imagenes",
      type: "info",
      html: body,
      allowOutsideClick: false,
      showCancelButton: true,
      cancelButtonText: 'Cancelar'
    }).then(function (data) {
      if (data) {
        //var row = $(obj).closest('tr');
        //$(row).addClass('completedImg');
        //var rowsCompletedsTxt = $(row).hasClass('completedTxt');
        //if (!rowsCompletedsTxt) {
        //    swal({
        //        title: 'Atencion!',
        //        text: 'Aun no ha capturado texto en esta partida, desea omitir la captura de texto?',
        //        type: 'question',
        //        allowOutsideClick: false,
        //        showCancelButton: true
        //    }).then(function (result) {
        //        if (result) {
        //            $(row).addClass('completedTxt');
        //            $(row).css('background-color', 'green');
        //            $(row).css('color', 'white');
        //            $(row).addClass('completedTotal');
        //            var rowsQuant = $('#tablaOCs tbody tr');
        //            var rowsCompleted = $('#tablaOCs tbody tr.completedTotal');
        //            if (rowsCompleted.length == rowsQuant.length) {
        //$('#tablaOCs').DataTable().ajax.reload(function () {
        //    $('body').removeClass('load-ajax');
        //    $('body .lean-overlay').remove();
        //$('#showModImgsBtn').removeAttr('data-ocoriginal');
        //$('#showModImgsBtn').removeAttr('data-ocrevision');
        //$('#showModImgsBtn').removeAttr('data-revision');
        //setRevisionCedis(purchaseOrderNumber);
        //})
        //            }
        //        }
        //    }, function (dismiss) {
        //        if (dismiss == 'cancel') {
        //            console.log('Seguiran capturando texto.');
        //            $(row).css('background-color', '#ffc107');
        //        }
        //    })
        //} else {
        //    $(row).addClass('completedTxt');
        //    $(row).css('background-color', 'green');
        //    $(row).css('color', 'white');
        //    $(row).addClass('completedTotal');
        //    var rowsQuant = $('#tablaOCs tbody tr');
        //    var rowsCompleted = $('#tablaOCs tbody tr.completedTotal');
        //    if (rowsCompleted.length == rowsQuant.length) {
        //        $('#tablaOCs').DataTable().ajax.reload(function () {
        //            $('body').removeClass('load-ajax');
        //            $('body .lean-overlay').remove();
        //            //setRevisionCedis(purchaseOrderNumber);
        //        });
        //    }
        //}
        swal({
          title: 'Atencion!',
          text: 'Ha terminado de capturar? Si da click en aceptar ya no podra volver a capturar',
          type: 'question',
          allowOutsideClick: false,
          showCancelButton: true,
          cancelButtonText: 'Continuar carga de imagenes',
          confirmButtonText: 'Terminar captura'
        }).then(function (data2) {
          if (data2) {
            //$('#tablaOCs').DataTable().ajax.reload(function () {
            //    $('body').removeClass('load-ajax');
            //    $('body .lean-overlay').remove();
            //    $('#showModImgsBtn').removeAttr('data-ocoriginal');
            //    $('#showModImgsBtn').removeAttr('data-ocrevision');
            //    $('#showModImgsBtn').removeAttr('data-revision');
            //    //setRevisionCedis(purchaseOrderNumber);
            //});
            sendMailCompras(purchaseOrderNumber, revision);
            setTimeout(() => {
              location.reload();
            }, 5000);
          }
        }, function (dismiss) {
          if (dismiss == 'cancel') {
            console.log('Seguiran capturando imagen1.');
          }
        });
      }
    }, function (dismiss) {
      if (dismiss == 'cancel') {
        console.log('Seguiran capturando imagen.');
      }
    }
    );
    showDropZone(purchaseOrderNumber, revision);
  }

  /*** Funcion para mostrar el modal de insercion de texto
   * @param {any} purchaseOrderNumber
   * @param {any} lineNumber
   * @param {any} itemNumber
  ***/
  function showModalText(purchaseOrderNumber, lineNumber, itemNumber, description, obj) {
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
    html += '		    <div class="col-md-6 lote">';
    html += '               <label class="form-label">Lote:</label>';
    html += '			    <input type="text" class="loteGen" data-index="0" id="loteGen0" style="width:100%;margin-top:5%"/>';
    html += '		    </div>';
    html += '		    <div class="col-md-6 coment">';
    html += '               <label class="form-label">Comentario:</label>';
    html += '			    <input type="text" class="comentGen" data-index="0" id="comentGen0" style="width:100%;margin-top:5%"/>';
    html += '		    </div>';
    html += '        </div>';
    html += '	</div>';
    html += '	<div class="col-md-2">';
    html += '		<i class="fa fa-plus" aria-hidden="true" style="cursor:pointer" onclick="addNewLineloteComent();"></i>';
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
    swal({
      type: 'info',
      html: html,
      title: 'Capture los lotes y comentarios',
      allowOutsideClick: false,
      allowEnterKey: false,
      preConfirm: function (result) {
        var purchaseOrderNumber = $('#purchaseOrderNumberHidden').val();
        var lineNumber = $('#lineNumberHidden').val();
        var itemNumber = $('#itemNumberHidden').val();
        var data = { purchaseOrderNumber, lineNumber, itemNumber };
        var validoLote = true;
        var lotArr = $('.loteGen');
        $(lotArr).each(function () {
          if (this.value == '') {
            validoLote = false;
            $(this).css('border', 'solid 2px red');
          }
        });
        return new Promise(function (resolve, reject) {
          if (!validoLote) {
            reject('El campo no puede ir vacio.');
          }
          resolve(data);
        });
      }
    }).then(function (result) {
      var Detalle = getDataLoteComentCedis();
      //*** Ajax para guardar los datos de los comentarios
      url = $('#urlSaveImagesComent').data('request-url');
      DataOrden = result;
      $.ajax({
        url: url,
        type: 'POST',
        dataType: "JSON",
        data: { DataLote: Detalle.DataLote, ComentImagen: Detalle.ComentImagen, DataOrden },
        beforeSend: function () {
          $('body').attr('data-msj', 'Guardando Datos ...');
          $('body').addClass('load-ajax');
          $('body').append('<div class="lean-overlay"></div>');
        },
        success: function (data) {
          $('body').removeClass('load-ajax');
          $('body .lean-overlay').remove();
          var row = $(obj).closest('tr');
          $(row).addClass('completedTxt');
          var rowsCompletedsImg = $(row).hasClass('completedImg');
          if (!rowsCompletedsImg) {
            swal({
              title: 'Atencion!',
              text: 'Aun no han capturado imagenes en esta partida, desea omitir la captura de imagenes?',
              type: 'question',
              allowOutsideClick: false,
              showCancelButton: true,
              cancelButtonText: 'Continuar capturando.',
              confirmButtonText: 'Omitir captura.',
            }).then(function (result) {
              if (result) {
                $(row).addClass('completedImg');
                $(row).css('background-color', 'green');
                $(row).css('color', 'white');
                $(row).addClass('completedTotal');
                var rowsQuant = $('#tablaOCs tbody tr');
                var rowsCompleted = $('#tablaOCs tbody tr.completedTotal');
                if (rowsCompleted.length == rowsQuant.length) {
                  $('#tablaOCs').DataTable().ajax.reload(function () {
                    $('body').removeClass('load-ajax');
                    $('body .lean-overlay').remove();
                    setRevisionCedis(DataOrden.purchaseOrderNumber);
                  });
                }
              }
            }, function (dismiss) {
              if (dismiss == 'cancel') {
                console.log('Seguiran capturando imagen.');
                $(row).css('background-color', '#ffc107');
              }
            })
          }
          else {
            $(row).css('background-color', 'green');
            $(row).css('color', 'white');
            $(row).addClass('completedTotal');
            var rowsQuant = $('#tablaOCs tbody tr');
            var rowsCompleted = $('#tablaOCs tbody tr.completedTotal');
            if (rowsCompleted.length == rowsQuant.length) {
              $('#tablaOCs').DataTable().ajax.reload(function () {
                $('body').removeClass('load-ajax');
                $('body .lean-overlay').remove();
                setRevisionCedis(DataOrden.purchaseOrderNumber);
              });
            }
          }
        },
        error: function (xHrRes) {
          console.log(xHrRes);
        }
      });
    });
  }


  /*** Funcion para mostrar el dropzone
   * @param {any} purchaseOrderNumber
   * @param {any} lineNumber
   * @param {any} itemNumber
   ***/
  function showDropZone(purchaseOrderNumber, revision) {
    var myDropzone = new Dropzone('.dropzone', {
      dictDefaultMessage: 'Arrastre archivos aqui para subirlos',
      dictRemoveFile: 'Eliminar Archivo',
      addRemoveLinks: true,
      duplicateCheck: true,
    });

    myDropzone.on('complete', function (res) {
      if (typeof res.xhr !== 'undefined') {
        if (res.xhr.responseText == '"Imagen duplicada"') {
          myDropzone.removeFile(res);
          $('.msg-error').html('<div class="alert alert-danger msg-error" role="alert"><button type="button" class="close" data-dismiss="alert" aria-label="Close"><span aria-hidden="true">&times;</span></button><br/>No es posible insertar imagenes duplicadas para la misma linea.</div >');
        }
      }
    });

    purchaseOrderNumber = $('#showModImgsBtn').data('ocrevision');
    revision = $('#showModImgsBtn').data('revision');

    myDropzone.on('removedfile', function (res) {
      if (typeof res.xhr !== 'undefined') {
        if (res.xhr.responseText != '"Imagen duplicada"') {
          removeDropFiles(myDropzone, res, 'fresca', purchaseOrderNumber, 0, 'N/A');
        }
      } else {
        removeDropFiles(myDropzone, res, 'fresca', purchaseOrderNumber, 0, 'N/A');
      }
    });

    recuperarDocs(purchaseOrderNumber, revision, myDropzone);
  }

  /*** Funcion para recuperar los documentos
   *** guardados en el server
   ***/
  function recuperarDocs(purchaseOrderNumber, revision, myDropzone) {
    url = $('#urlRecuperarDocs').data('request-url');
    $.ajax({
      url: url,
      type: 'POST',
      data: { purchaseOrderNumber },
      success: function (res) {
        $(res.mockfiles).each(function () {
          myDropzone.emit("addedfile", this);
          myDropzone.options.thumbnail.call(myDropzone, this, this.url + '/' + this.name);
          myDropzone.emit("complete", this);
          $('.dz-image img').css('width', '120px');
          $('.dz-image img').css('height', '120px');
        });
      }
    });
  }

  /*** Funcion para eliminar imagenes duplicadas
   *** thumbnail y fescas o existentes en el server
   ***/
  function removeDropFiles(myDropzone, file, method, purchaseOrderNumber, lineNumber, itemNumber) {
    if (method == 'duplicada') {
      myDropzone.removeFile(file);
    } else {
      url = $('#urlRemoveDocs').data('request-url');
      if (typeof file.xhr !== 'undefined') {
        response = JSON.parse(file.xhr.responseText);
        if (response.estatus == 'Exito!.') {
          idCodigos = response.idCodigosImagen;
        } else {
          idCodigos = file.idCodigosImagen;
        }
      } else {
        idCodigos = file.idCodigosImagen;
      }
      $.ajax({
        url: url,
        type: 'POST',
        data: { purchaseOrderNumber, lineNumber, itemNumber, idCodigos },
        success: function (res) {
        }
      });
    }
  }

  /*** Funcion para traer la lista de oc
  **** capturadas por compras
  ***/
  function getListaOCCapturadas() {
    url = $('#urlGetListaOCs').data('request-url');
    $.ajax({
      url: url,
      type: 'POST',
      data: {},
      success: function (res) {
        setSelectizeOptions('#ordenCompraCapturada', res.data);
      },
      error: function (xHR) {
        console.log(xHR);
      }
    });
  }

  /*** Funcion para cargar opciones de
  **** selectize
  ***/
  function setSelectizeOptions(select, optArr) {
    var select = $(select).selectize({
      valueField: 'value',
      labelField: 'text',
      searchField: 'value',
      options: optArr,
      persist: false,
      create: false,
      selectedField: "selected",
      items: ["0"]
    });
  }

  /** funcion para obtener los datos
  *** de los comentarios y las lineas de los lotes
  *** para su procesamiento en la db
  **/
  function getDataLoteComentCedis() {
    var lotArr = $('.loteGen');
    var comentImg = $('.comentImgGen')
    var data = { DataLote: [], ComentImagen: [] };
    $(lotArr).each(function () {
      index = $(this).data('index');
      lote = this.value;
      loteAtp = '';
      comentario = $('#comentGen' + index).val();
      comentarioATP = '';
      data.DataLote.push({ lote, comentario, loteAtp, comentarioATP });
    });
    $(comentImg).each(function () {
      var imgNum = $(this).attr('id');
      var nombre = $(this).data('nombre');
      data.ComentImagen.push({ comentario: this.value, imagen: imgNum, nombre });
    });
    return data;
  }

  /*** funcion para setear la revision
  **** a la ultima que se vaya a usar
  ***/
  function setRevisionCedis(oc) {
    var url = $('#urlSetRevision').data('request-url');
    $.ajax({
      url: url,
      type: 'POST',
      data: { oc },
      success: function (res) {
      }
    });
  }

  /*** Funcion para mandar correo de captura de
  **** orden de compra dirigida a compras con la captura de imagenes
  ***/
  function sendMailCompras(oc, revision) {
    var url = $('#urlSendMailCompras').data('request-url');
    $.ajax({
      url: url,
      type: 'POST',
      dataType: "JSON",
      data: { oc, revision },
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

  /**** Funcion para mostrar el div de cantidad de subrevision
  ****/
  function subrevisionLinea(check) {
    var row = $(check).closest('tr');
    var canti = $(row).find('.canti');
    var cantSR = $(row).find('.cantiSubRev');
    if ($(check).is(':checked')) {
      $(canti).hide();
      $(cantSR).show();
    } else {
      $(canti).show();
      $(cantSR).hide();
    }
  }

  /***** funcion para hacer la subrevision de lineas
  *****/
  function setSubrevisionLinea() {
    swal({
      title: 'Atencion!',
      text: 'Se va a hacer una subrevison de la OC, esta seguro que desea continuar?.',
      type: 'warning',
      allowOutsideClick: false,
      allowEnterKey: false,
      showCancelButton: true,
      cancelButtonText: 'Cancelar',
      confirmButtonText: 'Guardar Subrevision'
    }).then(function (result) {
      if (result) {
        url = $('#urlSetSubRevision').data('request-url');
        data = getDataSubRev();
        $.ajax({
          url: url,
          type: 'POST',
          dataType: "JSON",
          data: { subRev: data.data, revision: data.revision },
          beforeSend: function () {
            $('body').attr('data-msj', 'Guardando Datos ...');
            $('body').addClass('load-ajax');
            $('body').append('<div class="lean-overlay"></div>');
          },
          success: function (data) {
            swal("Exito!", "Operacion realizada existosamente!", "success");
            $('#tablaOCs').DataTable().ajax.reload();
            $('body').removeClass('load-ajax');
            $('body .lean-overlay').remove();
            var ocData = $('#ordenCompraCapturada').val().split('-');
            var oc = 'OC-' + ocData[0];
            var rev = ocData[1];
            var ocRev = oc + '-Rev' + rev;
            uploadImgsSub(oc, rev, ocRev)
          }
        });
      }
    });
  }

  /***** funcion para obtener los datos 
  ****** de la subrevison con los checks
  *****/
  function getDataSubRev() {
    var lineas = $('#tablaOCs').dataTable().fnGetNodes();
    var ocData = $('#ordenCompraCapturada').val().split('-');
    console.log(ocData)
    var company = companyCedis($('#ordenCompraCapturada').val());
    if (company == "atp") {
      var oc = 'OC-' + ocData[0];
      var rev = ocData[1];
      var dataSubRev = [];
    } else {
      var oc = 'L-' + ocData[1];
      var rev = ocData[2];
      var dataSubRev = [];
    }
    $(lineas).each(function () {
      var checkSubRev = $(this).find('.chkSubRev');
      if ($(checkSubRev).is(':checked')) {
        var lineNumber = $(this).find('td')[0];
        var codigo = $(this).find('td')[1];
        var divCanti = $(this).find('.cantiSubRev');
        var canti = $(divCanti).find('input').val();
        dataSubRev.push({
          lineNumber: $(lineNumber).html(),
          purchaseOrderNumber: oc,
          itemNumber: $(codigo).html(),
          orderedPurchaseQuantity: canti
        });
      }
    });

    return { data: dataSubRev, revision: rev };
  }

  /***** funcion para subir las imagenes en la revision
  *****/
function uploadImgsSub(purchaseOrderNumber, revision, purchaseOrderNumberRev) {
  var formData = new FormData();
  var images = $('.imgsSubRev')[0].files;
  formData.append('purchaseOrderNumber', purchaseOrderNumber);
  formData.append('revision', revision);
  formData.append('purchaseOrderNumberRev', purchaseOrderNumberRev);
  $(images).each(function (index) {
    formData.append('file' + index, this);
  });
  $.ajax({
    url: "Cedis/uploadFiles",
    type: "POST",
    data: formData,
    mimeType: "multipart/form-data",
    contentType: false,
    cache: false,
    processData: false,
    success: function (msg) {
      console.log(msg)
    }
  });
}

function companyCedis(str) {
  var regLin = '^L-OC[0-9]{6}-[0-9]$'
  var regAtp = '^[0-9]{9}-[0-9]$'
  var company = 'atp';
  if (str.match(regLin)){
    //alert('lin')
    company='lin'
  } else if (str.match(regAtp)) {
    //alert('atp')
    company='atp'
  } else {
    company = null;
  }
  return company
}