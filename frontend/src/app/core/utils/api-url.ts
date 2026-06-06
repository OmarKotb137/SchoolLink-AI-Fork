import { environment } from '../../../environments/environment';

type EnvironmentWithApi = typeof environment & {
  apiBaseUrl?: string;
  apiUrl?: string;
};

function getConfiguredBaseUrl(): string {
  const env = environment as EnvironmentWithApi;
  return (env.apiBaseUrl ?? env.apiUrl ?? '').trim();
}

function trimTrailingSlashes(value: string): string {
  return value.replace(/\/+$/, '');
}

function joinUrl(base: string, path: string): string {
  const cleanPath = path.replace(/^\/+/, '');
  if (!base) return cleanPath ? `/${cleanPath}` : '';
  return cleanPath ? `${base}/${cleanPath}` : base;
}

export function getApiBaseUrl(): string {
  const configured = trimTrailingSlashes(getConfiguredBaseUrl());
  if (!configured) return '/api';
  return configured.toLowerCase().endsWith('/api') ? configured : `${configured}/api`;
}

export function getBackendBaseUrl(): string {
  const configured = trimTrailingSlashes(getConfiguredBaseUrl());
  if (!configured) return '';
  return configured.toLowerCase().endsWith('/api')
    ? configured.slice(0, -4)
    : configured;
}

export function buildApiUrl(path = ''): string {
  return joinUrl(getApiBaseUrl(), path);
}

export function buildBackendUrl(path = ''): string {
  return joinUrl(getBackendBaseUrl(), path);
}
