(function () {
    "use strict";

    var input = document.getElementById("buscar");
    if (!input) { return; }

    var chips = Array.prototype.slice.call(document.querySelectorAll("[data-filter]"));
    var cards = Array.prototype.slice.call(document.querySelectorAll(".modulo-tarjeta"));
    var groups = Array.prototype.slice.call(document.querySelectorAll(".group"));
    var vacio = document.getElementById("sin-resultados");
    var contador = document.getElementById("contador");
    var activo = "todos";

    // Quita tildes y pasa a minusculas para buscar sin importar la escritura
    function limpiar(texto) {
        var t = (texto || "").toLowerCase();
        if (t.normalize) {
            t = t.normalize("NFD").replace(/[\u0300-\u036f]/g, "");
        }
        return t;
    }

    function aplicar() {
        var q = limpiar(input.value).trim();
        var visibles = 0;

        cards.forEach(function (card) {
            var okGrupo = activo === "todos" || card.getAttribute("data-group") === activo;
            var okTexto = q === "" || limpiar(card.getAttribute("data-texto")).indexOf(q) !== -1;
            var mostrar = okGrupo && okTexto;
            card.hidden = !mostrar;
            if (mostrar) { visibles++; }
        });

        groups.forEach(function (g) {
            g.hidden = g.querySelectorAll(".modulo-tarjeta:not([hidden])").length === 0;
        });

        if (contador) { contador.textContent = visibles + (visibles === 1 ? " modulo" : " modulos"); }
        if (vacio) { vacio.hidden = visibles !== 0; }
    }

    chips.forEach(function (chip) {
        chip.addEventListener("click", function () {
            activo = chip.getAttribute("data-filter");
            chips.forEach(function (c) {
                var esActivo = c === chip;
                c.classList.toggle("is-active", esActivo);
                c.setAttribute("aria-pressed", esActivo ? "true" : "false");
            });
            aplicar();
        });
    });

    input.addEventListener("input", aplicar);
    input.addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); aplicar(); } });

    document.addEventListener("keydown", function (e) {
        var enCampo = document.activeElement === input;
        if (e.key === "/" && !enCampo) {
            e.preventDefault();
            input.focus();
        } else if (e.key === "Escape" && enCampo) {
            input.value = "";
            aplicar();
            input.blur();
        }
    });
})();