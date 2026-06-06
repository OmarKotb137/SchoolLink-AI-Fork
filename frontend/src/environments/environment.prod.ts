const apiBaseUrl = 'http://localhost:5002/api'; // TODO: استبدل برابط API الحقيقي في production

export const environment = {
  production: true,
  apiBaseUrl,
  apiUrl: apiBaseUrl.replace(/\/api$/, '')
};
