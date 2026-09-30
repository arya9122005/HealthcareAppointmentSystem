// Fade out success/info alerts after a few seconds.
document.querySelectorAll('.alert-success[data-autohide]').forEach(function (el) {
    setTimeout(function () {
        el.style.transition = 'opacity .5s';
        el.style.opacity = '0';
        setTimeout(function () { el.remove(); }, 500);
    }, 4500);
});

// Dim schedule fields for days that are switched off.
document.querySelectorAll('[data-day-toggle]').forEach(function (box) {
    var target = document.getElementById(box.getAttribute('data-day-toggle'));
    function sync() { target.classList.toggle('off', !box.checked); }
    box.addEventListener('change', sync);
    sync();
});
