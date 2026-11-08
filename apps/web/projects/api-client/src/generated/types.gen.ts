export type ClientOptions = {
  baseUrl: 'https://localhost:7080/' | (string & {});
};

export type CsrfToken = {
  token: string;
  headerName: string;
};

export type CurrentUser = {
  id: string;
  displayName: string;
  email: string;
  state: string;
  emailConfirmed: boolean;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type ProblemDetails = {
  type?: null | string;
  title?: null | string;
  status?: null | number | string;
  detail?: null | string;
  instance?: null | string;
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

export type GetApiV1AuthCsrfData = {
  body?: never;
  path?: never;
  query?: never;
  url: '/api/v1/auth/csrf';
};

export type GetApiV1AuthCsrfResponses = {
  200: CsrfToken;
};

export type GetApiV1AuthCsrfResponse = GetApiV1AuthCsrfResponses[keyof GetApiV1AuthCsrfResponses];

export type PostApiV1AuthLoginData = {
  body: LoginRequest;
  path?: never;
  query?: never;
  url: '/api/v1/auth/login';
};

export type PostApiV1AuthLoginErrors = {
  400: ProblemDetails;

  401: ProblemDetails;
};

export type PostApiV1AuthLoginError = PostApiV1AuthLoginErrors[keyof PostApiV1AuthLoginErrors];

export type PostApiV1AuthLoginResponses = {
  200: CurrentUser;
};

export type PostApiV1AuthLoginResponse =
  PostApiV1AuthLoginResponses[keyof PostApiV1AuthLoginResponses];

export type GetApiV1AuthMeData = {
  body?: never;
  path?: never;
  query?: never;
  url: '/api/v1/auth/me';
};

export type GetApiV1AuthMeErrors = {
  401: ProblemDetails;
};

export type GetApiV1AuthMeError = GetApiV1AuthMeErrors[keyof GetApiV1AuthMeErrors];

export type GetApiV1AuthMeResponses = {
  200: CurrentUser;
};

export type GetApiV1AuthMeResponse = GetApiV1AuthMeResponses[keyof GetApiV1AuthMeResponses];

export type PostApiV1AuthLogoutData = {
  body?: never;
  path?: never;
  query?: never;
  url: '/api/v1/auth/logout';
};

export type PostApiV1AuthLogoutResponses = {
  204: void;
};

export type PostApiV1AuthLogoutResponse =
  PostApiV1AuthLogoutResponses[keyof PostApiV1AuthLogoutResponses];

export type PostApiV1AuthLogoutAllData = {
  body?: never;
  path?: never;
  query?: never;
  url: '/api/v1/auth/logout-all';
};

export type PostApiV1AuthLogoutAllResponses = {
  204: void;
};

export type PostApiV1AuthLogoutAllResponse =
  PostApiV1AuthLogoutAllResponses[keyof PostApiV1AuthLogoutAllResponses];
