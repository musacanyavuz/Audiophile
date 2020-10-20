var maxCountChar = 2000;
function countChar(val) {
    var len = val.value.length;
    if (len >= maxCountChar) {
        val.value = val.value.substring(0, maxCountChar);
    } else {
        $('.charNum').text(maxCountChar - len);
    }
};