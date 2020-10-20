

$( document ).ready(function() {
    
    $('.sss .accordion').on('click',function(){
        console.log(this);
        
        $(this).toggleClass('active');
        $($(this).parent()).find('.panel').toggleClass('show');        
    });
    
});

function getMoneyType(id)
{
    switch (id) {
        case 1:
            return 'TL';
        case 2:
            return 'USD';
        case 3:
            return 'EUR';
        case 4:
            return 'GBP';
    }
}


function letterControl(that, event) {
    const checkTurkishCharacters = (
        event.keyCode === 73 ||
            event.keyCode === 191 ||
            event.keyCode === 220 ||
            event.keyCode === 186 ||
            event.keyCode === 221 ||
            event.keyCode === 219);
    if (that.selectionStart === 0 &&
        (event.keyCode >= 65 && event.keyCode <= 90 || checkTurkishCharacters) &&
        !(event.shiftKey) &&
        !(event.ctrlKey) &&
        !(event.metaKey) &&
        !(event.altKey)) {
        const $t = $(that);
        event.preventDefault();
        //const char = String.fromCharCode(event.keyCode);
        const char = event.key.toUpperCase();
        $t.val(char + $t.val().slice(that.selectionEnd));
        that.setSelectionRange(1, 1);
    }
    else if (that.selectionStart > 0 &&
        (event.keyCode >= 65 && event.keyCode <= 90 || checkTurkishCharacters) &&
        !(event.shiftKey) &&
        !(event.ctrlKey) &&
        !(event.metaKey) &&
        !(event.altKey)) {
        const length = parseInt($(that).val().length);
        event.preventDefault();
        const char = event.key;
        if (length !== that.selectionStart) {
            console.log(that.selectionStart);
            const tempSelectionStart = that.selectionStart;
            $(that).val($(that).val().toString().splice(that.selectionStart, 0, char.toLowerCase()));
            that.setSelectionRange(tempSelectionStart + 1, tempSelectionStart + 1);
        } else {
            $(that).val($(that).val() + char.toLowerCase());
        }
    }
}