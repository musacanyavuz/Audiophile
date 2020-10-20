var helper = {
    FormControlSahis: function () {
        var form = $('.form-error-sahis');
        var fCntrl = false;

        form.each(function () {
            var c = $(this)[0];
            if (c.type == "checkbox" && !c.checked) {
                fCntrl = true;
            }
            else {
                var inputVal = $(this).val();
                if (inputVal == null || inputVal == "") {
                    $(this).addClass('has-error');
                    fCntrl = true;
                } else {
                    $(this).removeClass('has-error');
                }
            }
        });
        return fCntrl;
    },
    FormControlTuzel: function () {
        var form = $('.form-error-tuzel');
        var fCntrl = false;

        form.each(function () {
            var inputVal = $(this).val();
            if (inputVal == null || inputVal == "") {
                $(this).addClass('has-error');
                fCntrl = true;
            } else {
                $(this).removeClass('has-error');
            }
        });
        return fCntrl;
    }
};

$(function () {
    //$(":input").mask();
    $("#cep_tel").mask("+99 (999) 999-9999");
    //$('#cep_tel').mask("(99) 999 999 9999");
    //var _sahisIban = $('.sahis-iban').val();
    //var _sahisEmail = $('.sahis-email').val();
    //if (_sahisIban != "" && _sahisEmail != "") {
    //    $('.sahis-iban').attr('disabled', 'disabled');
    //    $('.sahis-email').attr('disabled', 'disabled');
    //}
    //var _tuzelIban = $('.tuzel-iban').val();
    //var _tuzelEmail = $('.tuzel-email').val();
    //if (_tuzelIban != "" && _tuzelEmail != "") {
    //    $('.tuzel-iban').attr('disabled', 'disabled');
    //    $('.tuzel-email').attr('disabled', 'disabled');
    //}
});

function formTuzelControl() {
    var _tuzel = validateIBAN($('.tuzel-iban').val());
    var _email = validateEmail($('.tuzel-email').val());

    if (!_tuzel) {
        alert("Lütfen IBAN Bilgisini Eksiksiz Giriniz.");
        return false;
    }
    if (!_email) {
        alert("Lütfen Geçerli Bir E-Mail Adresi Griiniz");
        return false;
    }

    if (helper.FormControlTuzel()) {
        alert("Lütfen * ile belirtilmiş alanları işaretleyiniz/doldurunuz.");
        return false;
    }
    $('#btnFormTuzel').trigger('click');
    return false;
}
function formSahisControl() {
    var _sahis =validateIBAN($('.sahis-iban').val());
    var _email = validateEmail($('.sahis-email').val());
    if (!_sahis) {
        alert("Lütfen IBAN Bilgisini Eksiksiz Giriniz.");
        return false;
    }
    if (!_email) {
        alert("Lütfen Geçerli Bir E-Mail Adresi Griiniz");
        return false;
    }
    if (helper.FormControlSahis()) {
        alert("Lütfen * ile belirtilmiş alanları işaretleyiniz/doldurunuz.");
        return false;
    }
    $('#btnFormSahis').trigger('click');
    return false;
}

function validateIBAN(iban) {
    if (iban.length !== 26 && iban.length !== 24 ) {
        return false;
    }
    return true;
}

function validateEmail(email) {
    var re = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    return re.test(email.toLowerCase());
}