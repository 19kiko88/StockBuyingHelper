export interface IResultDto<T> {
    id: string;
    success: boolean;
    message: string;
    content: T;
    exception?: object;
    innerResults?: IResultDto<T>[];
  }
  