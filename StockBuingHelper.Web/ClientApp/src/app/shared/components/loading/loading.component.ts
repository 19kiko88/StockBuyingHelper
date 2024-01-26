import { Component } from '@angular/core';
import { LoadingService } from './loading.service';

@Component({
  selector: 'app-loading',
  templateUrl: './loading.component.html',
  styleUrls: ['./loading.component.scss']
})
export class LoadingComponent {

  loader: boolean = false;  
  loadingMsg?: string = '';

  constructor(
    private _loadingService: LoadingService
  ) { }

  ngOnInit(): void {
    this._loadingService.loader$.subscribe(res =>{
      this.loader = res.isLoading;
      this.loadingMsg = res.loadingMessage;
    })
  }

}