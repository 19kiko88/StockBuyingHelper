import { Component, OnInit } from '@angular/core';
import { LoadingService } from './shared/components/loading/loading.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})

export class AppComponent implements OnInit {
  title = 'ClientApp';

  isLoader: boolean = false;
  loadingMsg: string|undefined = '';

  constructor
  (
    private _loadingService: LoadingService
  )
  {

  }

  ngOnInit(): void 
  {
          //Setting Loading
          this._loadingService.loader$.subscribe(res => {
            this.isLoader = res.isLoading;
            this.loadingMsg = res.loadingMessage;
          })  
  }
}
