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
                $('#uploadProgess').css({'width': text}).text(text);
            },
            queuecomplete: function () {
                $('#uploadProgess').css({'width': '0%'}).text('');
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
}

fileshare.flightcheck();
$(document).ready(fileshare.run);