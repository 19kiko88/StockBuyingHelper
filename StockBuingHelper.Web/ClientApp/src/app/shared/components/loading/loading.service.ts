import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {

  private emitLoader = new Subject<LoadingInfo>();
  loader$ = this.emitLoader.asObservable();

  constructor() { }

  setLoading(isLoading: boolean, loadingMessage?: string)
  {
    if(!loadingMessage)
    {
      loadingMessage = 'Loading...';
    }

    let data: LoadingInfo = { isLoading: isLoading, loadingMessage: loadingMessage}
    this.emitLoader.next(data);
  }

}

export interface LoadingInfo{
  isLoading: boolean;
  loadingMessage?: string;
}

