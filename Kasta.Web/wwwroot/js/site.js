var fileshare = {
    flightcheck: () => {
        Dropzone.options.uploadDropzone = {
            paramName: 'file',
            maxFilesize: window.UserLimits.MaxUploadQuota / Math.pow(1024, 2),
            error: function (file, response) {
                this.defaultOptions.error(file, response.message);
            },
            totaluploadprogress: function (uploadProgress) {
                var text = Math.round(uploadProgress) + '%';
                $('#uploadProgress').css({'width': text}).text(text);
            },
            queuecomplete: function () {
                $('#uploadProgress').css({'width': '0%'}).text('');
            },
            success: function (file, response) {
                $(file.previewElement)
                    .find('.dz-filename')
                    .children()
                    .html('<a href="' + response.url + '">' + file.name + '</a>');
            },
            timeout: 0
        }
    },
    run: () => {
        if ($('.dropzone').length > 0) {
            fileshare.initClipboardPasteUpload();
        }
        $('a').filter(function() {return $(this).attr('data-clipboard-text')}).on('click', e => {
            navigator.clipboard.writeText($(e.currentTarget).attr('data-clipboard-text'));
            alert('Copied to clipboard');
        });
    },
    initClipboardPasteUpload: function() {
        document.onpaste = function(event){
            if (event.clipboardData || event.originalEvent.clipboardData) {
                const items = (event.clipboardData || event.originalEvent.clipboardData).items;
                items.forEach((item) => {
                    if (item.kind === 'file') {
                        Dropzone.forElement('.dropzone').addFile(item.getAsFile());
                    }
                });
            }
        }
    },
    createLinkPage: function() {
        $("#createLinkModal button[type=submit]").click(() => {
            var destinationElem = $("#createLinkModal #text-destination");
            var useVanityElem = $("#createLinkModal #checkbox-vanity");
            var vanityElem = $("#createLinkModal #text-vanity");
            var valid = destinationElem.length > 0 && useVanityElem.length > 0 && vanityElem.length > 0;
            if (valid)
            {
                var req = {
                    destination: destinationElem[0].value
                };
                if (useVanityElem[0].checked)
                {
                    var v = vanityElem[0].value.trim();
                    if (v.length > 1)
                    {
                        req.vanity = v;
                    }
                }
                console.log(req);
                function cleanupElements() {
                    destinationElem.forEach(function (elem) {
                        elem.value = "";
                    });
                    if (useVanityElem && useVanityElem.length > 0)
                    {
                        useVanityElem.forEach(function (elem) {
                            elem.checked = false;
                        });
                    }
                    if (vanityElem && vanityElem.length > 0)
                    {
                        vanityElem.forEach(function (elem) {
                            elem.value = "";
                        });
                    }
                }
                var opts = {
                    url: '/api/v1/Link/Create',
                    method: 'POST',
                    data: req,
                    dataType: 'json'
                };
                $.ajax(opts).done(function (data, textStatus, xhrResponse) {
                    console.debug('[SUCCESS] Created Link', xhrResponse);
                    if (textStatus === 'success')
                    {
                        if (data.url)
                        {
                            console.debug(`Generated URL: ${data.url}`);
                            navigator.clipboard.writeText(data.url);
                            alert("Copied link to clipboard");
                            window.location.reload();
                        }
                        else
                        {
                            console.warn('Malformed response. Url property not found!', xhrResponse.responseText);
                            alert("Malformed response (missing property 'url')");
                        }
                    }
                }).fail(function (xhrResponse, textStatus, errorThrown) {
                    console.error('Failed to create link', xhrResponse, errorThrown);
                    var msg = xhrResponse.responseText;
                    if (xhrResponse.responseJSON)
                    {
                        if (xhrResponse.responseJSON.message)
                        {
                            msg = xhrResponse.responseJSON.message;
                        }
                        else
                        {
                            msg = JSON.stringify(xhrResponse.responseJSON);
                        }
                    }
                    if (msg.trim().length > 0)
                    {
                        msg = "\nMessage: " + msg;
                    }
                    else
                    {
                        msg = "";
                    }
                    alert(`Failed to create link. (${xhrResponse.status}, ${textStatus})` + msg);
                });
            }
        });
    }
}

fileshare.flightcheck();
$(document).ready(fileshare.run);