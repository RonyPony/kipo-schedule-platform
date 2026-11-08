import { readdir, readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const directory = fileURLToPath(new URL('../projects/api-client/src/generated/', import.meta.url));
for (const name of await readdir(directory)) {
  if (!name.endsWith('.ts')) continue;
  const file = path.join(directory, name);
  const text = await readFile(file, 'utf8');
  await writeFile(file, text.replace(/^\/\/[^\n]*\n/gm, '').replace(/\/\*\*[\s\S]*?\*\//g, ''), 'utf8');
}
