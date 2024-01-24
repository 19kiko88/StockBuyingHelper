import { Injectable } from '@angular/core';
import { IResultDto } from '../../core/dtos/response/result-dto';

@Injectable({
  providedIn: 'root'
})
export class BaseService {

  constructor() { }

  // private
  protected generateGetOptions(): {} 
  {
    const params: any = { v: new Date().getTime().toString() };
    return {
      params,
      withCredentials: true,
    };
  }

  protected generatePostOptions(): {} {
    return {
      // headers: new HttpHeaders({
      //   'Content-Type': 'application/json',
      // }),
      withCredentials: true,
    };
  }

  protected buildFormData(
    formData: FormData,
    data: any,
    parentKey?: string
  ): void {
    if (
      data &&
      typeof data === 'object' &&
      !(data instanceof Date) &&
      !(data instanceof File)
    ) {
      Object.keys(data).forEach((key) => {
        this.buildFormData(
          formData,
          data[key],
          parentKey ? `${parentKey}[${key}]` : key
        );
      });
    } else {
      if (!parentKey) {
        throw new Error('buildFormData fail: no parentKey');
      }

      const value = data == null ? '' : data;
      formData.append(parentKey, value);
    }
  }

  protected processResult<T>(result: IResultDto<T>): T {
    if (result) {
      if (result.success) {
        return result.content;
      } else {
        throw new Error(result.message);
      }
    }

    throw new Error('無法處理取得之回傳結果');
  } 
}
