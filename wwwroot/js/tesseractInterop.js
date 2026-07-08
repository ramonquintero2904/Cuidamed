// Asegúrate de incluir la librería en tu index.html: 
// <script src="https://cdn.jsdelivr.net/npm/tesseract.js@5/dist/tesseract.min.js"></script>

window.BlazorOcr = {
    analizarDocumento: async function (imageSrc, lang = 'spa') {
        try {
            // Creamos el worker de Tesseract configurado en español
            const worker = await Tesseract.createWorker(lang);

            // Realizamos el reconocimiento de la imagen (acepta URL base64 o blob URL)
            const { data: { text } } = await worker.recognize(imageSrc);

            // Destruimos el worker para liberar memoria en el navegador
            await worker.terminate();

            // Normalizamos el texto para facilitar la validación de palabras clave
            const textoNormalizado = text.toLowerCase();

            // Criterios de validación (puedes expandir esta lista)
            const esValido = textoNormalizado.includes("recipe") ||
                textoNormalizado.includes("récipe") ||
                textoNormalizado.includes("diagnostico") ||
                textoNormalizado.includes("diagnóstico") ||
                textoNormalizado.includes("rp:") ||
                textoNormalizado.includes("indicaciones");

            return {
                exito: true,
                textoExtraido: text,
                esDocumentoValido: esValido
            };
        } catch (error) {
            console.error("Error en Tesseract.js:", error);
            return {
                exito: false,
                textoExtraido: "",
                esDocumentoValido: false
            };
        }
    }
};