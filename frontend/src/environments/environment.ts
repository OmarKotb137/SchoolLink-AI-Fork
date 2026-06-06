const apiBaseUrl = 'http://localhost:5002/api';

export const environment = {
  production: false,
  apiBaseUrl,
  apiUrl: apiBaseUrl.replace(/\/api$/, '')
};
