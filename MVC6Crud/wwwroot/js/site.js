function DisplayLoader() {
    $('#Loader').show();
}

function HideLoader() {
    $('#Loader').hide();
}
$('#Loader').hide();

$(document).on('submit', 'form', function () {
    DisplayLoader();
})

$(window).on('beforeunload', function () {
    DisplayLoader();
})



$(function () {
    if ($('div.alert.msg').length) {
        debugger
        setTimeout(() => {
            $('div.alert.msg').fadeOut();
        }, 4000);
    }
});