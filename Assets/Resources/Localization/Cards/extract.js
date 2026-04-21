const fs = require('fs');
const path = require('path');

const dir = 'c:\\Users\\bambi\\Documents\\TADAK_TCG\\Assets\\Resources\\Localization\\Cards';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.json'));

let descs = [];

for (const file of files) {
    const data = JSON.parse(fs.readFileSync(path.join(dir, file), 'utf8').replace(/^\uFEFF/, ''));
    if (data.cards) {
        data.cards.forEach(card => {
            if (card.koDescription) {
                descs.push({
                    file: file,
                    id: card.cardId,
                    name: card.koName,
                    desc: card.koDescription
                });
            }
        });
    }
}

// Group by semantic text structure (replacing amounts)
let grouped = {};
descs.forEach(c => {
    let raw = c.desc;
    // Replace numbers and variables with placeholders to group similar ones
    // Actually, let's just use raw desc for now, maybe just replacing {.*?} with {}
    let template = raw.replace(/\{[a-zA-Z0-9_]+\}/g, '{}');
    if (!grouped[template]) {
        grouped[template] = new Set();
    }
    grouped[template].add(raw);
});

// To help the LLM find inconsistencies, let's sort templates and output them
let output = [];
for (let template in grouped) {
    let variations = Array.from(grouped[template]);
    output.push(`${template}`);
    variations.forEach(v => {
        if (v !== template) {
            output.push(`  -> ${v}`);
        }
    });
}

output.sort();
fs.writeFileSync(path.join(dir, 'ko_desc_analysis.txt'), output.join('\n'));
console.log(`Extracted ${output.length} unique templates.`);
