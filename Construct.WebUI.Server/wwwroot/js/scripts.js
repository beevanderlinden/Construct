

console.log("✅ scripts.js loaded!");


// Accordion
window.toggleAccordion = function (accordionId) {
    var acc = document.getElementById(accordionId);
    acc.classList.toggle("active");
    var panel = acc.nextElementSibling;
    if (panel.style.display === "block") {
        panel.style.display = "none";
        
    } else {
        panel.style.display = "block";
    }
};

// Scroll to Table of Contents
window.scrollToToc = function () {
    var elem = document.getElementById('toc-embvg01f');
    if (elem) elem.scrollIntoView({ behavior: 'smooth' });
};

// Scroll to bookmark (for internal document links)
window.scrollToBookmark = function (bookmarkId) {
    // Zoek eerst naar <a name='bookmarkId'> (klassieke HTML bookmark)
    var elem = document.querySelector('[name="' + bookmarkId + '"]');
    
    // Als niet gevonden, zoek naar id='bookmarkId' (moderne HTML)
    if (!elem) elem = document.getElementById(bookmarkId);
    
    if (elem) {
        // Als het element in een gesloten accordion zit, open die eerst
        var panel = elem.closest('.ec-panel');
        
        if (panel) {
            // Check computed style (werkt ook als display via CSS class wordt gezet)
            var computedStyle = window.getComputedStyle(panel);
            
            if (computedStyle.display === 'none') {
                panel.style.display = 'block';
                var accordion = panel.previousElementSibling;
                if (accordion && accordion.classList.contains('ec-accordion')) {
                    accordion.classList.add('active');
                }
            }
        }
        
        elem.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
};

// File download function
window.downloadFile = (fileName, contentType, base64Data) => {
    const link = document.createElement('a');
    link.href = `data:${contentType};base64,${base64Data}`;
    link.download = fileName;
    link.click();
};


window.openInNewTab = function (url) {
    window.open(url, '_blank');
};



