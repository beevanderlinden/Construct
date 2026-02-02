window.dirInterop = window.dirInterop || {};

// ✅ Vraag permanente opslagrechten aan (optioneel, verbetert gebruikerservaring)
window.dirInterop.requestPersistentStorage = async () => {
    try {
        if ('storage' in navigator && 'persist' in navigator.storage) {
            const granted = await navigator.storage.persist();
            if (!granted) {
                console.warn("Gebruiker heeft permanente opslag geweigerd.");
            }
            return granted;
        } else {
            console.warn("Persistent storage wordt niet ondersteund door deze browser.");
        }
    } catch (err) {
        console.error("Fout bij aanvragen permanente opslag:", err);
    }
    return false;
};

// ✅ Map kiezen en opslaan in sessionStorage (of IndexedDB)
window.dirInterop.selectAndStoreDirectory = async () => {
    const handle = await window.showDirectoryPicker();
    window.dirInterop._lastDirHandle = handle;
    const permission = await handle.requestPermission({ mode: 'readwrite' });

    if (permission !== 'granted') throw new Error("Geen toegang tot map verleend.");

    // Opslaan in sessionStorage voor herstel
    const id = await window.dirInterop.storeHandle(handle);
    sessionStorage.setItem("dirHandleId", id);
};

// ✅ Herstel gekozen map uit vorige sessie (indien beschikbaar)
window.dirInterop.restoreDirectory = async () => {
    const id = sessionStorage.getItem("dirHandleId");
    if (!id) return false;

    const handle = await window.dirInterop.restoreHandle(id);
    if (!handle) return false;

    const permission = await handle.requestPermission({ mode: 'readwrite' });
    if (permission !== 'granted') return false;

    window.dirInterop._lastDirHandle = handle;
    return true;
};

// ✅ Gebruiker kiest expliciet een andere map (vervangt vorige handle)
window.dirInterop.chooseNewDirectory = async () => {
    try {
        const handle = await window.showDirectoryPicker();
        const permission = await handle.requestPermission({ mode: 'readwrite' });

        if (permission !== 'granted') {
            throw new Error("Geen toegang tot nieuwe map verleend.");
        }

        window.dirInterop._lastDirHandle = handle;

        // Eventueel nieuwe opslag in sessionStorage of IndexedDB
        const id = await window.dirInterop.storeHandle(handle);
        sessionStorage.setItem("dirHandleId", id);

        return true;
    } catch (err) {
        console.error("Fout bij kiezen nieuwe map:", err);
        return false;
    }
};

// ✅ Geef de mapnaam van de huidige directory terug
window.dirInterop.getSelectedDirectoryName = () => {
    const handle = window.dirInterop._lastDirHandle;
    if (!handle) return null;
    return handle.name; // meestal de mapnaam, niet het volledige pad
};




// ✅ Lees alle .cprj-bestanden (geen submappen)
window.dirInterop.openDirectoryAndReadCprjFiles = async () => {
    let dirHandle = window.dirInterop._lastDirHandle;

    if (!dirHandle) {
        try {
            dirHandle = await window.showDirectoryPicker();
            const permission = await dirHandle.requestPermission({ mode: 'readwrite' });
            if (permission !== 'granted') throw new Error("Geen toegang tot map verleend.");
            window.dirInterop._lastDirHandle = dirHandle;
        } catch (err) {
            console.error("Fout bij selecteren map:", err);
            throw new Error("Geen map geselecteerd of toegang geweigerd.");
        }
    }

    const files = [];
    for await (const entry of dirHandle.values()) {
        if (entry.kind === "file" && entry.name.endsWith(".cprj")) {
            const file = await entry.getFile();
            const arrayBuffer = await file.arrayBuffer();
            const base64 = btoa(String.fromCharCode(...new Uint8Array(arrayBuffer)));

            files.push({
                Name: file.name,
                Base64: base64,
                Size: file.size,
                LastModified: file.lastModified
            });
        }
    }

    // Sorteer op laatste wijzigingsdatum (aflopend)
    files.sort((a, b) => b.lastModified - a.lastModified);

    return files;
};

// ✅ Bestand opslaan via showSaveFilePicker
window.dirInterop.saveCprjFile = async (defaultFileName, base64content) => {
    const options = {
        suggestedName: defaultFileName.endsWith('.cprj') ? defaultFileName : `${defaultFileName}.cprj`,
        types: [{
            description: 'CProject File',
            accept: { 'application/x-cprj': ['.cprj'] }
        }]
    };

    const handle = await window.showSaveFilePicker(options);
    let fileName = handle.name;

    // ✅ Controleer of de extensie er echt op zit
    if (!fileName.toLowerCase().endsWith('.cprj')) {
        fileName += '.cprj';
    }

    const writable = await handle.createWritable();

    const binary = atob(base64content);
    const buffer = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        buffer[i] = binary.charCodeAt(i);
    }

    await writable.write(buffer);
    await writable.close();

    return fileName; // ✅ Je kunt dit eventueel teruggeven aan Blazor

    //const options = {
    //    suggestedName: defaultFileName,
    //    types: [{ description: 'CProject', accept: { 'application/octet-stream': ['.cprj'] } }]
    //};

    //const handle = await window.showSaveFilePicker(options);
    //const writable = await handle.createWritable();

    //const binary = atob(base64content);
    //const buffer = new Uint8Array(binary.length);
    //for (let i = 0; i < binary.length; i++) {
    //    buffer[i] = binary.charCodeAt(i);
    //}

    //await writable.write(buffer);
    //await writable.close();
};

// ✅ Bestand automatisch opslaan in geselecteerde map (overschrijft indien nodig)
window.dirInterop.autoSaveCprjFile = async (fileName, base64content) => {
    const dirHandle = window.dirInterop._lastDirHandle;
    if (!dirHandle) throw new Error("Geen map geselecteerd");

    try {
        const fileHandle = await dirHandle.getFileHandle(fileName, { create: true });
        const writable = await fileHandle.createWritable();

        const binary = atob(base64content);
        const buffer = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            buffer[i] = binary.charCodeAt(i);
        }

        await writable.write(buffer);
        await writable.close();
        console.info(`Bestand ${fileName} succesvol opgeslagen.`);
    } catch (err) {
        console.error(`Fout bij automatisch opslaan van ${fileName}:`, err);
        throw err;
    }
};

// ✅ IndexedDB storage voor directory handles
window.dirInterop.storeHandle = async (handle) => {
    const db = await navigator.storage.getDirectory();
    const id = crypto.randomUUID();
    const permission = await handle.requestPermission({ mode: 'readwrite' });
    if (permission !== 'granted') throw new Error("Geen toestemming voor handle opslag");
    await window.indexedDBHelper.store(id, handle);
    return id;
};

window.dirInterop.restoreHandle = async (id) => {
    return await window.indexedDBHelper.get(id);
};

// ✅ Eenvoudige IndexedDB helper via idb-keyval (vereist extra script of eigen implementatie)
window.indexedDBHelper = {
    async getDb() {
        return new Promise((resolve, reject) => {
            const open = indexedDB.open("DirHandles", 1);
            open.onupgradeneeded = () => {
                open.result.createObjectStore("handles");
            };
            open.onsuccess = () => resolve(open.result);
            open.onerror = () => reject(open.error);
        });
    },

    async store(key, value) {
        const db = await this.getDb();
        const tx = db.transaction("handles", "readwrite");
        tx.objectStore("handles").put(value, key);
        return tx.complete;
    },

    async get(key) {
        const db = await this.getDb();
        const tx = db.transaction("handles", "readonly");
        return tx.objectStore("handles").get(key);
    }
};
