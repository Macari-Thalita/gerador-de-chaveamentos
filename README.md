# Gerador de Chaveamentos

Primeiro MVP de um sistema desktop em C# para importar inscrições de campeonato, agrupar atletas por categoria e gerar chaveamentos simples.

## O que esta versão já faz

- Importa arquivo `.xlsx`, `.txt` ou `.tsv` com as colunas do Google Forms.
- Identifica as colunas pelo cabeçalho, então a ordem pode mudar.
- Ignora colunas extras do Forms, como carimbo de data/hora e email.
- Normaliza alguns textos, como gênero e graduação.
- Agrupa atletas por idade, gênero, peso e graduação.
- Permite revisar dados importados antes de gerar as categorias.
- Cria uma chave simples usando a próxima potência de 2.
- Exporta chave atual ou todas as chaves para PDF.
- Desenha o PDF em formato de chaveamento para impressão e preenchimento manual.

## Requisitos

- Windows
- .NET 8 SDK instalado
- Visual Studio 2022 ou Rider

## Como abrir

Abra a pasta `BjjBracketManager` no Visual Studio e execute o projeto `BjjBracketManager`.

Também dá para executar pelo terminal dentro da pasta do projeto:

```bash
dotnet restore
dotnet run --project BjjBracketManager
```

## Formato esperado da planilha

A primeira linha deve ter cabeçalhos que identifiquem estas informações:

1. Nome do atleta
2. Gênero
3. Peso
4. Graduação
5. Equipe
6. Categoria de idade

As colunas podem estar em qualquer ordem. O app também aceita colunas extras, como `Carimbo de data/hora` e `email`.
