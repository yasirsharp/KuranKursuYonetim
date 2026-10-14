/** Ortak API yanıt sarmalayıcısı */
export interface IApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

/** Sayfalı liste yanıtı (Backend PagedResult<T>) */
export interface IPaginatedResponse<T> {
  data: T[];
  items?: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

