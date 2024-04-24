import { AdminService } from 'src/app/core/http/admin.service';
import { Component } from '@angular/core';
import { ResGetHistoryDto } from 'src/app/core/dtos/response/res-get-history-dto';

@Component({
  selector: 'app-history',
  templateUrl: './history.component.html',
  styleUrl: './history.component.css'
})
export class HistoryComponent  {

  histories!: ResGetHistoryDto[];
  cols!: Column[];

  constructor
  (
    private _adminService: AdminService 
  )
  {

  }


  ngOnInit()
  {
    this._adminService.GetHistory().subscribe(
      {
        next: res => {
          this.histories = res.content;
        }
      }
    )

    this.cols = [      
      { field: 'content', header: '歷程記錄' },
      { field: 'createUser', header: '新增人員' },
      { field: 'createDate', header: '新增日期' },
  ];
  }
}

interface Column {
  field: string;
  header: string;
}
