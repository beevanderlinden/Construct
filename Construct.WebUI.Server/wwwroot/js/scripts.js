
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

