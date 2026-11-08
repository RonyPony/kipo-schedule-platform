export type ClientOptions = {
  baseUrl: 'http://127.0.0.1:5080/' | (string & {});
};

export type ServiceStatus = {
  product: string;
  status: string;
  apiVersion: string;
};

export type GetServiceStatusData = {
  body?: never;
  path?: never;
  query?: never;
  url: '/api/v1/status';
};

export type GetServiceStatusResponses = {
  200: ServiceStatus;
};

export type GetServiceStatusResponse = GetServiceStatusResponses[keyof GetServiceStatusResponses];
