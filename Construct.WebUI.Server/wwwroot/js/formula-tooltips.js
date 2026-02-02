window.initFormulaTooltips = function () {

    tippy('.has-formula', {
        allowHTML: true,
        animation: 'scale',
        theme: 'light-border',
        placement: 'top',
        interactive: false,

        onShow(instance) {

            const tex = instance.reference.dataset.tex;
            const caption = instance.reference.dataset.caption;
            const displayMode = instance.reference.dataset.displayMode === 'true';

            if (!tex) return;

            const container = document.createElement('div');

            try {
                katex.render(tex, container, {
                    throwOnError: false,
                    displayMode: displayMode
                });
            }
            catch {
                container.textContent = tex;
            }

            if (caption) {
                const cap = document.createElement('div');
                cap.className = 'formula-caption';
                cap.textContent = caption;
                container.appendChild(cap);
            }

            instance.setContent(container);
        }
    });
};
