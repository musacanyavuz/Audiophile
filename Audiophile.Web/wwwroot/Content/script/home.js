$(function () {
    var owl = $('#banner_logo').addClass('owl-carousel');


    owl.owlCarousel({
        //loop: true,
        //margin: 10,
        //nav: false,
        //autoplay: true,
        dots: false,
        responsive: {
            0: {
                items: 1
            },
            600: {
                items: 3
            },
            1000: {
                items: 5
            }
        },
        loop: true,
        margin: 0,
        autoplay: true,
        smartSpeed: 3500
        //autoplayTimeout: 3000
    });


    $('.banner_logo div').on('click',
        function (e) {
            $(this).closest('.owl-carousel').trigger('play.owl.autoplay');
        });
     
});