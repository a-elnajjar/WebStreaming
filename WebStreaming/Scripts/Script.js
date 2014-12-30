/// <reference path="script.js" />
$(function () {



    $("#ddlcas").load(GetFiles());
    

    function GetFiles() {
        jQuery.support.cors = true;
        $.ajax({
            url: 'api/media/GetFiles',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                var appenddata;
                $.each(data, function (key, value) {
                    appenddata += "<option value = '" + value + " '>" + value + " </option>";
                });
                $('#ddlcas').html(appenddata);
            },
            error: function (x, y, z) {
                alert(x + '\n' + y + '\n' + z);
            }
        });
    }

    $("#ddlcas").change(function () {

        var selectedFile = $('#ddlcas').val().replace(/^.*[\\\/]/, '');
        $('#mainPlayer source').attr('src', 'api/media/play?f=' + selectedFile);;
        $('#mainPlayer')[0].load();
    });


 






});