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

// Render KaTeX formulas binnen een element
window.renderKatexInElement = function (selector) {
    const element = document.querySelector(selector);
    if (!element) return;
    
    // Zoek alle elementen met formulas
    const formulas = element.querySelectorAll('.katex-formula');
    
    formulas.forEach(formula => {
        // Skip if already rendered
        if (formula.classList.contains('katex-rendered')) {
            return;
        }
        
        const text = formula.textContent.trim();
        
        // Extract LaTeX from \(...\) delimiters
        let latex = text;
        if (text.startsWith('\\(') && text.endsWith('\\)')) {
            latex = text.substring(2, text.length - 2);
        }
        
        try {
            katex.render(latex, formula, {
                throwOnError: false,
                displayMode: false
            });
            // Mark as rendered
            formula.classList.add('katex-rendered');
        } catch (e) {
            console.error('KaTeX render error:', e);
            formula.textContent = latex;
        }
    });
};


