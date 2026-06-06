const apiBaseUrl = 'https://localhost:5001/api';

export const environment = {
  production: false,
  apiBaseUrl,
  apiUrl: apiBaseUrl.replace(/\/api$/, '')
};
