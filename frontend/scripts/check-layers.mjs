// Fails when a file imports from a layer it must not know about. Run by CI.
//
//   core         nothing else in the app
//   data-access  core
//   shared/ui    core
//   shared/*     core, data-access, shared
//   features     core, data-access, shared, layout/auth, and only their own feature folder
//   layout       core, data-access, shared
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join, relative, resolve, sep } from 'node:path';

const app = resolve('projects/bank-app/src/app');

const files = (dir) =>
  readdirSync(dir).flatMap((name) => {
    const path = join(dir, name);
    if (statSync(path).isDirectory()) return files(path);
    return name.endsWith('.ts') && !name.endsWith('.spec.ts') ? [path] : [];
  });

const location = (path) => relative(app, path).split(sep);

function allowed(from, to) {
  const [layer, area] = from;
  const [target, targetArea] = to;
  if (layer === target && layer !== 'features' && layer !== 'shared') return true;

  switch (layer) {
    case 'core':
      return false;
    case 'data-access':
      return target === 'core';
    case 'shared':
      if (area === 'ui') return target === 'core' || (target === 'shared' && targetArea === 'ui');
      return ['core', 'data-access', 'shared'].includes(target);
    case 'features':
      if (target === 'features') return targetArea === area;
      return ['core', 'data-access', 'shared'].includes(target) || (target === 'layout' && targetArea === 'auth');
    case 'layout':
      return ['core', 'data-access', 'shared'].includes(target);
    default:
      return true; // app.routes and config compose everything
  }
}

const problems = [];
for (const file of files(app)) {
  const from = location(file);
  for (const [, path] of readFileSync(file, 'utf8').matchAll(/from '(\.[^']+)'/g)) {
    const to = location(resolve(dirname(file), path));
    if (to[0] !== '..' && !allowed(from, to)) problems.push(`${from.join('/')} -> ${to.join('/')}`);
  }
}

if (problems.length > 0) {
  console.error(`Layer violations:\n  ${problems.join('\n  ')}`);
  process.exit(1);
}
console.log('Layers OK');
