import { Component, OnInit } from '@angular/core';
import { LoadingService } from './shared/components/loading/loading.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})

export class AppComponent {
  title = '買股小幫手';

  isLoader: boolean = false;
  loadingMsg: string|undefined = '';

  constructor() {}
}
