import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Le fichier qui va stocker notre numéro de build
const filePath = path.join(__dirname, 'src', 'build-number.json');

let buildData = { build: 0 };

// Si le fichier existe déjà, on lit le numéro actuel
if (fs.existsSync(filePath)) {
    const rawData = fs.readFileSync(filePath);
    buildData = JSON.parse(rawData);
}

// On incrémente le numéro de 1
buildData.build += 1;

// On sauvegarde le nouveau numéro dans le fichier
fs.writeFileSync(filePath, JSON.stringify(buildData, null, 2));

console.log(`✅ Numéro de build incrémenté à : ${buildData.build}`);