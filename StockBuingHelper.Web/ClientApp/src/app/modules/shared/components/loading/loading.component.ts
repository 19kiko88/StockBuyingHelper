import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading',
  templateUrl: './loading.component.html',
  styleUrls: ['./loading.component.scss']
})
export class LoadingComponent {

  @Input() loader: boolean = false;  
  @Input() loadingMsg: string|undefined = '';

  constructor() { }

  ngOnInit(): void {
    
  }

}

export interface LoadingInfo{
  isLoading: boolean;
  loadingMessage?: string;
}
