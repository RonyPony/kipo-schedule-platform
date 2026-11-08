import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: '../../docs/api/openapi.v1.json',
  output: { path: 'projects/api-client/src/generated' },
  plugins: ['@hey-api/typescript'],
});
