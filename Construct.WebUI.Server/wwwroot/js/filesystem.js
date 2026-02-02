console.log("✅ filesytem.js loaded!");




window.dirInterop = {

    _lastDirHandle: null,

    openDirectoryAndReadCprjFiles: async () => {
        const dirHandle = await window.showDirectoryPicker();
        const files = [];

        for await (const entry of dirHandle.values()) {
            if (entry.kind === "file" && entry.name.endsWith(".cprj")) {
                const file = await entry.getFile();
                const arrayBuffer = await file.arrayBuffer();



                const base64 = btoa(String.fromCharCode(...new Uint8Array(arrayBuffer)));

                files.push({
                    name: file.name,
                    base64: base64,
                    lastModified: file.lastModified,
                    size: file.size
                });
            }
        }

        window.dirInterop._lastDirHandle = dirHandle;
        return files;
    },

    autoSaveCprjFile: async (fileName, base64content) => {
        const dirHandle = window.dirInterop._lastDirHandle;
        if (!dirHandle) throw new Error("Directory not selected.");

        const fileHandle = await dirHandle.getFileHandle(fileName, { create: true });
        const writable = await fileHandle.createWritable();

        const binary = atob(base64content);
        const buffer = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            buffer[i] = binary.charCodeAt(i);
        }

        await writable.write(buffer);
        await writable.close();
    },


    saveCprjFile: async (defaultFileName, base64content) => {
        const options = {
            suggestedName: defaultFileName,
            types: [{ description: 'CProject', accept: { 'application/octet-stream': ['.cprj'] } }]
        };

        const handle = await window.showSaveFilePicker(options);
        const writable = await handle.createWritable();

        const binary = atob(base64content);
        const buffer = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            buffer[i] = binary.charCodeAt(i);
        }

        await writable.write(buffer);
        await writable.close();
    }

    
 
   

};
