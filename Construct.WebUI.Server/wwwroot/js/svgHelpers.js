window.svgHelpers = {

    /**
     * Geeft de data-attributes terug van het element op een bepaald schermpunt (clientX, clientY)
     * @param {number} clientX - pointer X in pixels
     * @param {number} clientY - pointer Y in pixels
     * @returns {object} - dictionary van data-attributes, bv { "load-id": "...", "handle": "start" }
     */
    getElementDataAttributesAtPoint: function (clientX, clientY) {
        const elem = document.elementFromPoint(clientX, clientY);
        if (!elem) return null;

        const data = {};
        for (const attr of elem.attributes) {
            if (attr.name.startsWith("data-")) {
                data[attr.name.slice(5)] = attr.value;
            }
        }
        return data;
    },


    getBoundingClientRect: function (id) {
        const el = document.getElementById(id);
        if (!el) return null;
        const rect = el.getBoundingClientRect();
        return { width: rect.width, height: rect.height };
    },
    registerResizeCallback: function (id, dotNetHelper) {
        const el = document.getElementById(id);
        if (!el) return;
        const observer = new ResizeObserver(entries => {
            for (let entry of entries) {
                dotNetHelper.invokeMethodAsync('OnContainerResize', entry.contentRect.width, entry.contentRect.height);
            }
        });
        observer.observe(el);
    },

    renderSvgToPng: function (svgXml, width, height) {
        return new Promise((resolve, reject) => {
            const blob = new Blob([svgXml], { type: "image/svg+xml;charset=utf-8" });
            const url = URL.createObjectURL(blob);

            const img = new Image();
            img.onload = () => {
                const canvas = document.createElement("canvas");
                canvas.width = width;
                canvas.height = height;
                canvas.getContext("2d").drawImage(img, 0, 0, width, height);
                URL.revokeObjectURL(url);
                resolve(canvas.toDataURL("image/png"));
            };
            img.onerror = () => reject("SVG kon niet geladen worden!");
            img.src = url;
        });
    },

    downloadSvgAsSvg: function (svgXml, fileName = "image.svg"){
        const blob = new Blob([svgXml], { type: "image/svg+xml;charset=utf-8" });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(link.href);
    },

    downloadSvgAsPng: function (svgXml, maxPixels = 400, fileName = "image.png") {
        // 1. Parse de viewBox uit de SVG
        const parser = new DOMParser();
        const doc = parser.parseFromString(svgXml, "image/svg+xml");
        const svgEl = doc.documentElement;
        let vb = svgEl.getAttribute("viewBox");
        if (!vb) {
            // fallback op width/height attribuut
            const w = parseFloat(svgEl.getAttribute("width")) || maxPixels;
            const h = parseFloat(svgEl.getAttribute("height")) || maxPixels;
            vb = `0 0 ${w} ${h}`;
        }

        const [_, __, vbWidth, vbHeight] = vb.split(" ").map(parseFloat);

        // 2. Bereken canvas grootte behoudend aspect ratio
        let canvasWidth, canvasHeight;
        if (vbWidth > vbHeight) {
            canvasWidth = maxPixels;
            canvasHeight = Math.round(maxPixels * (vbHeight / vbWidth));
        } else {
            canvasHeight = maxPixels;
            canvasWidth = Math.round(maxPixels * (vbWidth / vbHeight));
        }

        // 3. Blob -> Image -> Canvas
        const blob = new Blob([svgXml], { type: "image/svg+xml;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const img = new Image();
        img.onload = () => {
            const canvas = document.createElement("canvas");
            canvas.width = canvasWidth;
            canvas.height = canvasHeight;
            const ctx = canvas.getContext("2d");
            ctx.drawImage(img, 0, 0, canvasWidth, canvasHeight);

            canvas.toBlob((blobPng) => {
                const link = document.createElement("a");
                link.href = URL.createObjectURL(blobPng);
                link.download = fileName;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            }, "image/png");

            URL.revokeObjectURL(url);
        };
        img.onerror = () => console.error("SVG kon niet geladen worden!");
        img.src = url;
    },




    svgToBase64Png: function (svgXml, maxPixels = 400) {
        return new Promise((resolve, reject) => {
            try {

                // 0. Validaties
                // pas de style="..." aan, want deze moeten genegeerd worden bij rendering 
                svgXml = svgXml.replace(/style="[^"]*"/g, "");



                // 1. Parse de viewBox uit de SVG
                const parser = new DOMParser();
                const doc = parser.parseFromString(svgXml, "image/svg+xml");
                const svgEl = doc.documentElement;
                let vb = svgEl.getAttribute("viewBox");

                if (!vb) {
                    const w = parseFloat(svgEl.getAttribute("width")) || maxPixels;
                    const h = parseFloat(svgEl.getAttribute("height")) || maxPixels;
                    vb = `0 0 ${w} ${h}`;
                }

                const parts = vb.split(" ").map(parseFloat);
                const vbWidth = parts[2];
                const vbHeight = parts[3];

                // 2. Canvasgrootte met aspect ratio
                let canvasWidth, canvasHeight;
                if (vbWidth > vbHeight) {
                    canvasWidth = maxPixels;
                    canvasHeight = Math.round(maxPixels * (vbHeight / vbWidth));
                } else {
                    canvasHeight = maxPixels;
                    canvasWidth = Math.round(maxPixels * (vbWidth / vbHeight));
                }

                // 3. Blob -> Image -> Canvas -> Base64
                const blob = new Blob([svgXml], { type: "image/svg+xml;charset=utf-8" });
                const url = URL.createObjectURL(blob);
                const img = new Image();

                img.onload = () => {
                    const canvas = document.createElement("canvas");
                    canvas.width = canvasWidth;
                    canvas.height = canvasHeight;
                    const ctx = canvas.getContext("2d");
                    ctx.drawImage(img, 0, 0);

                    try {
                        const dataUrl = canvas.toDataURL("image/png");
                        const base64 = dataUrl;
                        //const base64 = dataUrl.replace(/^data:image\/png;base64,/, "");
                        resolve(base64);
                    } catch (err) {
                        reject("Kon PNG niet genereren: " + err);
                    }

                    URL.revokeObjectURL(url);
                };

                img.onerror = () => reject("SVG kon niet geladen worden.");
                img.src = url;
            } catch (e) {
                reject("Fout bij conversie: " + e.message);
            }
        });
    },


    convertToPng: function (svgXml, width = 1200, height = 800) {
        return new Promise((resolve, reject) => {
            const blob = new Blob([svgXml], { type: "image/svg+xml;charset=utf-8" });
            const url = URL.createObjectURL(blob);

            const img = new Image();
            img.onload = () => {
                try {
                    const canvas = document.createElement("canvas");
                    canvas.width = width;
                    canvas.height = height;
                    const ctx = canvas.getContext("2d");
                    ctx.drawImage(img, 0, 0, width, height);
                    URL.revokeObjectURL(url);
                    resolve(canvas.toDataURL("image/png"));
                } catch (err) {
                    reject(err);
                }
            };
            img.onerror = () => reject(new Error("SVG load failed"));
            img.src = url;
        });
    },

    downloadPng: function (base64Png, fileName = "test.png") {
        const link = document.createElement("a");
        link.href = base64Png;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },

    saveSvgToTemp: async function (svgXml, width, height, dotNetHelper) {
        try {
            const pngDataUrl = await window.svgHelpers.convertToPng(svgXml, width, height);
            const base64 = pngDataUrl.split(',')[1]; // strip 'data:image/png;base64,'
            await dotNetHelper.invokeMethodAsync('ReceivePngFromClient', base64);
        } catch (err) {
            console.error("Fout bij renderen of versturen van PNG:", err);
        }
    },

    convertToBase64Png: async function (svgXml, width = 1200, height = 800) {
        const dataUrl = await window.svgHelpers.convertToPng(svgXml, width, height);
        return dataUrl.split(',')[1]; // geeft alleen base64 string terug
    },


    setStatus: async function (svgId, status) {
        const svg = document.getElementById(svgId);
        if (!svg) return;

        // verwijder alle mogelijke status-classes
        svg.classList.remove("warning", "error", "success");

        // voeg nieuwe status toe (indien opgegeven)
        if (status) {
            svg.classList.add('${status}');
        }
    }


};



window.SvgInterop = {
    svgToBase64Bitmap: async function (svgXml, width, height, format = "png", timeoutMs = 5000) {
        return new Promise((resolve, reject) => {
            try {
                if (!svgXml) return reject("empty svg");

                // 1) Base64-encodeer de SVG veilig (handle UTF-8)
                const svg64 = btoa(unescape(encodeURIComponent(svgXml)));
                const dataUrl = "data:image/svg+xml;base64," + svg64;

                const img = new Image();

                // 2) voorkom canvas tainting bij externe resources: probeer anonymous, maar externe resources kunnen nog steeds falen
                img.crossOrigin = "anonymous";

                const timer = setTimeout(() => {
                    img.onload = null;
                    img.onerror = null;
                    reject("SVG->Image load timeout");
                }, timeoutMs);

                img.onload = function () {
                    try {
                        clearTimeout(timer);

                        // Create canvas with requested size
                        const canvas = document.createElement("canvas");
                        canvas.width = Math.max(1, Math.floor(width));
                        canvas.height = Math.max(1, Math.floor(height));
                        const ctx = canvas.getContext("2d");

                        // draw image scaled to canvas
                        ctx.clearRect(0, 0, canvas.width, canvas.height);
                        ctx.drawImage(img, 0, 0, canvas.width, canvas.height);

                        // Export. If canvas is tainted, this will throw.
                        const mime = `image/${format}`;
                        const dataUrlOut = canvas.toDataURL(mime);
                        resolve(dataUrlOut.replace(/^data:image\/\w+;base64,/, ""));
                    } catch (err) {
                        reject("Error drawing or exporting canvas: " + (err && err.message ? err.message : err));
                    }
                };

                img.onerror = function (ev) {
                    clearTimeout(timer);
                    reject("Image load error (invalid SVG or external resources blocked).");
                };

                // 3) Start load
                img.src = dataUrl;
            } catch (ex) {
                reject("Unexpected error: " + ex.message);
            }
        });
    }
};




window.SvgInteropBAK = {
    svgToBase64Bitmap: async function (svgXml, width, height, format = "png") {
        return new Promise((resolve, reject) => {
            try {
                const blob = new Blob([svgXml], { type: "image/svg+xml" });
                const url = URL.createObjectURL(blob);
                const img = new Image();
                img.onload = function () {
                    const canvas = document.createElement("canvas");
                    canvas.width = width;
                    canvas.height = height;
                    const ctx = canvas.getContext("2d");
                    ctx.drawImage(img, 0, 0, width, height);
                    const dataUrl = canvas.toDataURL(`image/${format}`);
                    URL.revokeObjectURL(url);
                    resolve(dataUrl.replace(/^data:image\/\w+;base64,/, ""));
                };
                img.onerror = reject;
                img.src = url;
            } catch (e) {
                reject(e);
            }
        });
    }
};



window.svgToPng = {
    convertToPng: async function (svgXml, width = 1200, height = 800) {
        const blob = new Blob([svgXml], { type: "image/svg+xml;charset=utf-8" });
        const url = URL.createObjectURL(blob);

        const img = new Image();
        img.src = url;

        await img.decode();

        const canvas = document.createElement("canvas");
        canvas.width = width;
        canvas.height = height;

        const ctx = canvas.getContext("2d");
        ctx.drawImage(img, 0, 0, width, height);

        URL.revokeObjectURL(url);
        return canvas.toDataURL("image/png");
    },

    downloadPng: function (base64Png, fileName = "test.png") {
        const link = document.createElement("a");
        link.href = base64Png;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
};



window.svgExport = {
    svgToPng: async function (svgElementId) {
        const svgEl = document.getElementById(svgElementId);
        if (!svgEl) return null;

        const svgClone = svgEl.cloneNode(true);
        const svgData = new XMLSerializer().serializeToString(svgClone);
        const svgBlob = new Blob([svgData], { type: "image/svg+xml;charset=utf-8" });
        const url = URL.createObjectURL(svgBlob);

        return new Promise((resolve) => {
            const img = new Image();
            img.onload = () => {
                const canvas = document.createElement("canvas");
                canvas.width = img.width;
                canvas.height = img.height;
                const ctx = canvas.getContext("2d");
                ctx.drawImage(img, 0, 0);
                canvas.toBlob((blob) => {
                    URL.revokeObjectURL(url);
                    resolve(blob);
                }, "image/png");
            };
            img.onerror = () => resolve(null);
            img.src = url;
        });
    }
};

window.downloadFileFromBase64 = (fileName, base64) => {
    const link = document.createElement('a');
    link.href = `data:application/octet-stream;base64,${base64}`;
    link.download = fileName;
    link.click();
};
